/*
 * 102.0 — Relais de signalements : reçoit un rapport envoyé par l'application
 * et crée une issue GitHub à sa place.
 *
 * POURQUOI UN RELAIS ET PAS UN APPEL DIRECT DEPUIS L'APPLICATION
 * -------------------------------------------------------------
 * Créer une issue demande un jeton d'écriture. Un jeton embarqué dans un
 * exécutable distribué publiquement N'EST PAS UN SECRET : il s'extrait en
 * quelques secondes de n'importe quel binaire, et permettrait alors d'écrire
 * dans le dépôt au nom du mainteneur. Le jeton reste donc ici, côté serveur,
 * et l'application ne connaît qu'une URL — qu'on peut révoquer en changeant le
 * Worker, sans republier l'application.
 *
 * CE QUI N'EST PAS DÉFENDABLE, ET QU'IL FAUT SAVOIR
 * ------------------------------------------------
 * L'URL, elle, est extractible. N'importe qui peut donc poster ici. Aucun
 * secret partagé n'y changerait rien (il serait extractible aussi), et une
 * épreuve anti-robot type Turnstile suppose un navigateur. La défense est donc
 * assumée comme étant : quotas, bornes de taille, et modération humaine des
 * issues créées. En dernier recours, supprimer le Worker coupe le robinet
 * immédiatement, l'application retombant d'elle-même sur le chemin GitHub.
 *
 * AUCUNE DONNÉE PERSONNELLE N'EST DEMANDÉE NI STOCKÉE. Les issues créées sont
 * PUBLIQUES : l'application prévient l'utilisateur avant l'envoi, et aucun
 * champ de contact n'existe — le suivi se fait par l'URL de l'issue, renvoyée
 * à l'application. Les adresses IP ne sont jamais écrites : seul un condensé
 * salé et à durée de vie courte sert au comptage.
 */

const TYPES = {
  bug: { label: "bug", prefix: "[Bug]" },
  feature: { label: "enhancement", prefix: "[Idée]" },
  feedback: { label: "feedback", prefix: "[Retour]" },
};

// Marque tout ce qui arrive par ce chemin. C'est cette étiquette qui donne la
// « base » demandée : filtrer le dépôt dessus liste exactement les
// signalements reçus depuis le logiciel.
const SOURCE_LABEL = "via-application";

const LIMITS = {
  titleMin: 3,
  titleMax: 120,
  bodyMin: 20,
  bodyMax: 8000,
  versionMax: 32,
  contextMax: 400,
  requestMax: 16 * 1024,
  perIpPerHour: 3,
  perDay: 200,
};

export default {
  async fetch(request, env) {
    if (request.method === "OPTIONS") return new Response(null, { status: 204, headers: cors() });
    if (request.method !== "POST") return fail(405, "method_not_allowed", "Utilise POST.");

    const url = new URL(request.url);
    if (url.pathname !== "/report") return fail(404, "not_found", "Chemin inconnu.");

    // Longueur annoncée d'abord : refuser avant de lire évite de tirer un
    // corps de plusieurs mégaoctets pour le rejeter ensuite.
    const declared = Number(request.headers.get("content-length") || "0");
    if (declared > LIMITS.requestMax) return fail(413, "too_large", "Signalement trop volumineux.");

    let payload;
    try {
      const raw = await request.text();
      if (raw.length > LIMITS.requestMax) return fail(413, "too_large", "Signalement trop volumineux.");
      payload = JSON.parse(raw);
    } catch {
      return fail(400, "bad_json", "Corps de requête illisible.");
    }

    const problem = validate(payload);
    if (problem) return fail(400, problem.code, problem.message);

    const quota = await checkQuota(request, env);
    if (!quota.ok) return fail(429, quota.code, quota.message);

    const type = TYPES[payload.type];
    const title = `${type.prefix} ${clean(payload.title, LIMITS.titleMax)}`;

    const created = await createIssue(env, {
      title,
      body: buildBody(payload),
      labels: [SOURCE_LABEL, type.label],
    });

    if (!created.ok) {
      // Le détail de l'erreur GitHub reste dans les logs du Worker : le
      // renvoyer exposerait la configuration du dépôt à un appelant anonyme.
      console.error("Création d'issue refusée par GitHub", created.status, created.detail);
      return fail(502, "upstream", "Le service de signalement est indisponible. Réessaie plus tard.");
    }

    await consumeQuota(request, env);

    return json(200, { ok: true, url: created.url, number: created.number });
  },
};

function validate(p) {
  if (!p || typeof p !== "object") return { code: "bad_payload", message: "Corps de requête invalide." };
  if (!Object.prototype.hasOwnProperty.call(TYPES, p.type))
    return { code: "bad_type", message: "Type de signalement inconnu." };

  const title = clean(p.title ?? "", LIMITS.titleMax);
  if (title.length < LIMITS.titleMin) return { code: "title_too_short", message: "Titre trop court." };

  const body = clean(p.body ?? "", LIMITS.bodyMax);
  if (body.length < LIMITS.bodyMin) return { code: "body_too_short", message: "Description trop courte." };

  return null;
}

/**
 * Retire les caractères de contrôle et de formatage (mêmes catégories que
 * Services/SupportersProvider.cs côté application : U+202E et consorts peuvent
 * faire afficher un titre autrement qu'il n'est écrit), puis borne la
 * longueur. Les sauts de ligne sont préservés dans le corps, ils y ont un sens.
 */
function clean(value, max) {
  if (typeof value !== "string") return "";
  const stripped = value
    .replace(/\r\n?/g, "\n")
    .replace(/[\p{Cf}\p{Co}\p{Cn}]/gu, "")
    .replace(/[^\S\n]+/gu, " ")
    .replace(/\n{3,}/g, "\n\n")
    .trim();
  return Array.from(stripped).slice(0, max).join("").trim();
}

function buildBody(p) {
  const context = clean(p.context ?? "", LIMITS.contextMax);
  const version = clean(p.version ?? "", LIMITS.versionMax);

  // Le bandeau n'est pas décoratif : sans lui, une issue créée par le jeton du
  // mainteneur passerait pour un rapport écrit par lui, et personne ne saurait
  // qu'un tiers attend une réponse.
  return [
    "> Envoyé depuis l'application par un utilisateur, via le bouton « Signaler un bug ».",
    "> L'auteur n'a pas de compte GitHub : il suit cette issue par son adresse.",
    "",
    clean(p.body, LIMITS.bodyMax),
    "",
    "---",
    version ? `**Version** : ${version}` : null,
    context ? `**Contexte** : ${context}` : null,
  ]
    .filter((line) => line !== null)
    .join("\n");
}

/**
 * Deux plafonds, pour deux abus différents : quelques envois par heure et par
 * adresse arrêtent le clic répété, un plafond quotidien global arrête une
 * campagne distribuée que le premier ne verrait pas passer.
 *
 * L'adresse IP n'est jamais stockée : la clé est un condensé SHA-256 salé,
 * qui expire de lui-même. On ne peut donc ni remonter à un utilisateur ni
 * dresser un historique.
 */
async function checkQuota(request, env) {
  if (!env.REPORTS) return { ok: true }; // KV non configuré : pas de comptage.

  const ipKey = "ip:" + (await hashIp(request, env));
  const dayKey = "day:" + new Date().toISOString().slice(0, 10);

  const [ipCount, dayCount] = await Promise.all([
    env.REPORTS.get(ipKey).then((v) => Number(v || "0")),
    env.REPORTS.get(dayKey).then((v) => Number(v || "0")),
  ]);

  if (ipCount >= LIMITS.perIpPerHour)
    return { ok: false, code: "rate_limited", message: "Trop de signalements envoyés. Réessaie dans une heure." };
  if (dayCount >= LIMITS.perDay)
    return { ok: false, code: "daily_limit", message: "Le service a atteint sa limite du jour. Réessaie demain." };

  return { ok: true };
}

/**
 * Incrémenté APRÈS création de l'issue, jamais avant : une panne de GitHub ne
 * doit pas consommer le quota de quelqu'un dont le signalement n'est pas parti.
 */
async function consumeQuota(request, env) {
  if (!env.REPORTS) return;

  const ipKey = "ip:" + (await hashIp(request, env));
  const dayKey = "day:" + new Date().toISOString().slice(0, 10);

  const [ipCount, dayCount] = await Promise.all([
    env.REPORTS.get(ipKey).then((v) => Number(v || "0")),
    env.REPORTS.get(dayKey).then((v) => Number(v || "0")),
  ]);

  await Promise.all([
    env.REPORTS.put(ipKey, String(ipCount + 1), { expirationTtl: 3600 }),
    env.REPORTS.put(dayKey, String(dayCount + 1), { expirationTtl: 172800 }),
  ]);
}

async function hashIp(request, env) {
  const ip = request.headers.get("cf-connecting-ip") || "inconnue";
  const data = new TextEncoder().encode((env.IP_SALT || "sel-par-defaut") + "|" + ip);
  const digest = await crypto.subtle.digest("SHA-256", data);
  return [...new Uint8Array(digest)].slice(0, 16).map((b) => b.toString(16).padStart(2, "0")).join("");
}

async function createIssue(env, issue) {
  const response = await fetch(`https://api.github.com/repos/${env.GITHUB_REPO}/issues`, {
    method: "POST",
    headers: {
      Authorization: `Bearer ${env.GITHUB_TOKEN}`,
      Accept: "application/vnd.github+json",
      "User-Agent": "ChaturbateRecorder-ReportRelay",
      "Content-Type": "application/json",
    },
    body: JSON.stringify(issue),
  });

  if (!response.ok) {
    return { ok: false, status: response.status, detail: await response.text() };
  }

  const created = await response.json();
  return { ok: true, url: created.html_url, number: created.number };
}

function cors() {
  return {
    "Access-Control-Allow-Origin": "*",
    "Access-Control-Allow-Methods": "POST, OPTIONS",
    "Access-Control-Allow-Headers": "Content-Type",
  };
}

function json(status, payload) {
  return new Response(JSON.stringify(payload), {
    status,
    headers: { "Content-Type": "application/json; charset=utf-8", ...cors() },
  });
}

function fail(status, code, message) {
  return json(status, { ok: false, error: code, message });
}
