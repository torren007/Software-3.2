// Easter egg: el botón "Contactar a los creadores" (cualquier elemento con data-creadores)
// dispara un screamer a pantalla completa con sonido.
// - La página queda "lockeada" mientras suena el audio (teclado y clics bloqueados).
// - Cuando termina el sonido se puede cerrar con clic o Esc: nunca queda trabada para siempre.
(() => {
  'use strict';

  const GIF = '/media/screamer.gif';
  const AUDIO = '/media/screamer.mp3';
  const VOLUMEN = 0.7;          // 0 a 1. Bajalo si te explota los oídos en la sala
  const BLOQUEO_MAX_MS = 17000; // el audio dura ~15,7 s; este es el tope por si no avisa 'ended'

  if (!document.querySelector('[data-creadores]')) return; // en páginas sin el botón no hace nada

  let activo = false;
  let sonido = null;

  // Precarga cuando el navegador está libre (así el susto sale sin demora)
  const precargar = () => {
    new Image().src = GIF;
    sonido = new Audio(AUDIO);
    sonido.preload = 'auto';
    sonido.volume = VOLUMEN;
  };
  (window.requestIdleCallback || setTimeout)(precargar, 1500);

  function disparar(origen) {
    if (activo) return;
    activo = true;

    let bloqueado = true;
    let listo = false;
    let timer;

    const overlay = document.createElement('div');
    overlay.className = 'screamer';
    overlay.tabIndex = -1;
    overlay.setAttribute('role', 'alertdialog');
    overlay.setAttribute('aria-modal', 'true');
    overlay.setAttribute('aria-label', 'Sorpresa a pantalla completa. Se puede cerrar con Escape cuando termina el sonido.');

    const img = new Image();   // elemento nuevo => el GIF arranca desde el primer cuadro
    img.alt = '';
    img.src = GIF;

    const aviso = document.createElement('div');
    aviso.className = 'screamer__aviso';
    aviso.textContent = 'Clic o Esc para cerrar';
    aviso.hidden = true;

    overlay.append(img, aviso);
    document.body.append(overlay);
    document.body.classList.add('screamer-lock');
    overlay.focus();

    // Pantalla completa real (requiere el gesto del clic; si el navegador la niega, igual tapa la ventana)
    try { overlay.requestFullscreen?.({ navigationUI: 'hide' })?.catch(() => {}); } catch { /* ignorar */ }

    // Sonido
    sonido ??= new Audio(AUDIO);
    sonido.volume = VOLUMEN;
    sonido.currentTime = 0;
    sonido.addEventListener('ended', liberar, { once: true });
    sonido.play().catch(() => { clearTimeout(timer); timer = setTimeout(liberar, 3000); }); // sin audio: libera enseguida

    timer = setTimeout(liberar, BLOQUEO_MAX_MS);

    function liberar() {
      if (listo) return;
      listo = true;
      bloqueado = false;
      clearTimeout(timer);
      aviso.hidden = false;
      overlay.classList.add('screamer--listo');
    }

    function cerrar() {
      clearTimeout(timer);
      sonido.pause();
      document.removeEventListener('keydown', onKey, true);
      document.removeEventListener('fullscreenchange', onFs);
      if (document.fullscreenElement) document.exitFullscreen?.().catch(() => {});
      overlay.remove();
      document.body.classList.remove('screamer-lock');
      activo = false;
      origen?.focus?.();
    }

    function onKey(e) {
      if (bloqueado) { e.preventDefault(); e.stopPropagation(); return; }
      if (e.key === 'Escape') cerrar();
    }
    // Si el usuario sale de pantalla completa con Esc una vez liberado, cerramos también
    function onFs() { if (!document.fullscreenElement && listo) cerrar(); }

    document.addEventListener('keydown', onKey, true);
    document.addEventListener('fullscreenchange', onFs);
    overlay.addEventListener('click', () => { if (!bloqueado) cerrar(); });
    overlay.addEventListener('contextmenu', (e) => e.preventDefault());
  }

  document.addEventListener('click', (e) => {
    const boton = e.target.closest('[data-creadores]');
    if (boton) disparar(boton);
  });
})();
