// Bascule FR/EN pour les pages Markdown (features/screenshots/roadmap).
// Partage la même clé localStorage("lang") que index.html : la langue
// choisie sur une page reste cohérente en naviguant sur tout le site.
(function () {
  function apply(lang) {
    document.documentElement.lang = lang;
    var frEls = document.querySelectorAll(".lang-fr");
    var enEls = document.querySelectorAll(".lang-en");
    for (var i = 0; i < frEls.length; i++) frEls[i].style.display = lang === "fr" ? "" : "none";
    for (var i2 = 0; i2 < enEls.length; i2++) enEls[i2].style.display = lang === "en" ? "" : "none";
    var btn = document.getElementById("langToggle");
    if (btn) btn.textContent = lang === "fr" ? "English" : "Français";
    localStorage.setItem("lang", lang);
  }

  var saved = localStorage.getItem("lang");
  var initial = saved || (navigator.language && navigator.language.toLowerCase().indexOf("fr") === 0 ? "fr" : "en");
  apply(initial);

  var toggleButton = document.getElementById("langToggle");
  if (toggleButton) {
    toggleButton.addEventListener("click", function () {
      apply(document.documentElement.lang === "fr" ? "en" : "fr");
    });
  }
})();
