# Relais de signalements (102.0)

Reçoit un signalement envoyé depuis l'application et crée une issue GitHub à
sa place, pour que quelqu'un **sans compte GitHub** puisse signaler un bug.

Tourne sur Cloudflare Workers. Le palier gratuit couvre très largement l'usage
attendu (100 000 requêtes par jour ; ce service en fera quelques dizaines).

## Pourquoi un relais plutôt qu'un appel direct

Créer une issue exige un jeton d'écriture. **Un jeton placé dans un
exécutable distribué n'est pas un secret** : il s'extrait en quelques secondes
et permettrait d'écrire dans le dépôt au nom du mainteneur. Le jeton reste donc
côté serveur ; l'application ne connaît qu'une URL.

L'URL, elle, est extractible — c'est admis. La défense repose sur les quotas,
les bornes de taille et la modération des issues. Supprimer le Worker coupe
tout immédiatement, l'application retombant d'elle-même sur le chemin GitHub
classique.

## Ce qui n'est jamais collecté

- **Aucune adresse e-mail, aucun champ de contact.** Les issues créées sont
  publiques : y publier l'adresse de quelqu'un qui signale un bug sur un
  enregistreur de cams serait un tort réel. Le suivi se fait par l'URL de
  l'issue, que l'application affiche après l'envoi.
- **Aucune adresse IP stockée.** Le comptage utilise un condensé SHA-256 salé,
  qui expire au bout d'une heure.
- **Rien n'est envoyé sans que l'utilisateur l'ait vu** : l'application affiche
  le texte exact qui partira, et prévient que l'issue sera publique.

## Déploiement — huit étapes

Tout se fait depuis ce dossier.

### 1. Créer le jeton GitHub

Sur <https://github.com/settings/personal-access-tokens/new> — un jeton
**fine-grained**, pas un classique :

- **Repository access** : « Only select repositories » → `ChaturbateRecorder`
- **Permissions** → Repository permissions → **Issues : Read and write**
- Rien d'autre. Aucune autre permission n'est nécessaire, et chacune ajoutée
  serait une permission de plus à fuiter si le Worker était compromis.
- Expiration : la plus courte que tu acceptes de renouveler (elle se remplace
  en rejouant l'étape 5).

### 2. Installer les outils

```bash
npm install -g wrangler
```

### 3. Se connecter à Cloudflare

```bash
wrangler login
```

### 4. Créer l'espace de comptage

```bash
wrangler kv namespace create REPORTS
```

Reporte l'`id` affiché dans `wrangler.toml`, à la place de `A_REMPLIR`.

### 5. Poser les secrets

```bash
wrangler secret put GITHUB_TOKEN
```

```bash
wrangler secret put IP_SALT
```

`IP_SALT` est une chaîne aléatoire quelconque, à ne jamais publier : sans elle,
un condensé d'adresse IP se retrouverait par simple force brute (il n'y a que
quatre milliards d'adresses v4).

### 6. Déployer

```bash
wrangler deploy
```

L'URL affichée à la fin est celle à me transmettre — elle ressemble à
`https://chaturbate-recorder-reports.<ton-compte>.workers.dev`.

### 7. Créer les étiquettes dans le dépôt

Trois étiquettes doivent exister, sans quoi GitHub refuse la création :
`via-application`, `feedback`, et les habituelles `bug` / `enhancement` (déjà
présentes par défaut).

### 8. Vérifier que ça marche

```bash
curl -X POST https://chaturbate-recorder-reports.<ton-compte>.workers.dev/report -H "Content-Type: application/json" -d '{"type":"feedback","title":"Essai du relais","body":"Message d essai envoye pour verifier que le relais cree bien une issue. A fermer."}'
```

La réponse doit contenir l'URL de l'issue créée. **Ferme-la ensuite** : c'est
un essai, pas un signalement.

## Ce que l'application envoie

```json
{
  "type": "bug | feature | feedback",
  "title": "résumé en une ligne",
  "body": "description libre",
  "version": "1.34.1",
  "context": "Windows 11 · ffmpeg présent · mode avancé"
}
```

Réponse en cas de succès :

```json
{ "ok": true, "url": "https://github.com/.../issues/42", "number": 42 }
```

En cas de refus, `{ "ok": false, "error": "<code>", "message": "<phrase>" }`.
Codes possibles : `bad_json`, `bad_payload`, `bad_type`, `title_too_short`,
`body_too_short`, `too_large`, `rate_limited`, `daily_limit`, `upstream`.

## Plafonds

| Plafond | Valeur | Ce qu'il arrête |
| --- | --- | --- |
| Par adresse et par heure | 3 | Le clic répété, volontaire ou non |
| Global par jour | 200 | Une campagne distribuée, que le premier ne verrait pas |
| Taille de requête | 16 Ko | Un corps démesuré |
| Titre / description | 120 / 8000 caractères | Une issue illisible |

Ils se modifient dans `LIMITS`, en haut de `worker.js`. **Le quota n'est
consommé qu'après création réussie de l'issue** : une panne de GitHub ne doit
pas coûter son quota à quelqu'un dont le signalement n'est pas parti.
