/**
 * PDF Reader Script
 * Este script maneja la visualización de PDFs y el seguimiento del progreso de lectura
 */

// Variables globales
let pdfDoc = null;
let pageNum = 1;
let pageRendering = false;
let pageNumPending = null;
let canvas = null;
let ctx = null;
let scale = 1.5;
let documentoId = 0;
let totalPages = 0;
let progresoActual = null;
let startTime = null;
let caracteresProcesados = 0;
let sesionActiva = false;

// Token Anti-Forgery para las solicitudes AJAX
const antiForgeryToken = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

/**
 * Inicializa el visor de PDF
 * @param {number} docId - ID del documento
 * @param {number} numPages - Número total de páginas
 * @param {Object} progreso - Objeto con el progreso inicial
 */
function initPdfViewer(docId, numPages, progreso) {
    // Configuración de PDF.js
    pdfjsLib.GlobalWorkerOptions.workerSrc = 'https://cdnjs.cloudflare.com/ajax/libs/pdf.js/3.4.120/pdf.worker.min.js';

    // Referencias al DOM
    canvas = document.getElementById('pdfViewer');
    ctx = canvas.getContext('2d');

    // Variables de documento
    documentoId = docId;
    totalPages = numPages;
    progresoActual = progreso || {
        paginaActual: 1,
        indiceCaracter: 0,
        posicionX: 0,
        posicionY: 0,
        elementoId: '',
        fragmentoContexto: '',
        porcentajeCompletado: 0,
        textoCompletado: ''
    };

    // Iniciar desde la página guardada en el progreso
    pageNum = progresoActual.paginaActual || 1;

    // Cargar el documento PDF
    loadPDF();

    // Iniciar el seguimiento de la sesión
    startTracking();

    // Configurar botones de navegación
    document.getElementById('btnPrevPage').addEventListener('click', onPrevPage);
    document.getElementById('btnNextPage').addEventListener('click', onNextPage);
    document.getElementById('btnSaveProgress').addEventListener('click', saveProgress);

    // Añadir texto existente a las notas
    if (progresoActual.textoCompletado) {
        document.getElementById('userNotes').value = progresoActual.textoCompletado;
    }
}

/**
 * Carga el documento PDF desde el servidor
 */
function loadPDF() {
    // Mostrar indicador de carga
    document.getElementById('loadingIndicator').style.display = 'block';

    // Obtener el PDF del servidor
    const pdfUrl = `/PDFAdmin/ObtenerPDF/${documentoId}`;

    // Cargar el documento con PDF.js
    pdfjsLib.getDocument(pdfUrl).promise.then(function (pdf) {
        pdfDoc = pdf;

        // Actualizar información de páginas
        document.getElementById('paginaActual').textContent = `Página ${pageNum} de ${totalPages}`;

        // Renderizar la página actual
        renderPage(pageNum);

        // Ocultar indicador de carga
        document.getElementById('loadingIndicator').style.display = 'none';
    }).catch(function (error) {
        console.error('Error al cargar el PDF:', error);
        document.getElementById('loadingIndicator').textContent = 'Error al cargar el PDF: ' + error.message;
    });
}

/**
 * Renderiza una página específica del PDF
 * @param {number} num - Número de página a renderizar
 */
function renderPage(num) {
    pageRendering = true;

    // Obtener la página
    pdfDoc.getPage(num).then(function (page) {
        // Ajustar el tamaño del canvas al tamaño de la página
        const viewport = page.getViewport({ scale: scale });
        canvas.height = viewport.height;
        canvas.width = viewport.width;

        // Renderizar el PDF en el canvas
        const renderContext = {
            canvasContext: ctx,
            viewport: viewport
        };

        const renderTask = page.render(renderContext);

        // Cuando termine de renderizar
        renderTask.promise.then(function () {
            pageRendering = false;

            // Si hay una página pendiente, renderizarla
            if (pageNumPending !== null) {
                renderPage(pageNumPending);
                pageNumPending = null;
            }

            // Actualizar la información de la página
            document.getElementById('paginaActual').textContent = `Página ${pageNum} de ${totalPages}`;

            // Actualizar el progreso
            updateProgress();
        });
    });
}

/**
 * Cambiar a la página anterior
 */
function onPrevPage() {
    if (pageNum <= 1) {
        return;
    }
    pageNum--;
    queueRenderPage(pageNum);
}

/**
 * Cambiar a la página siguiente
 */
function onNextPage() {
    if (pageNum >= pdfDoc.numPages) {
        return;
    }
    pageNum++;
    queueRenderPage(pageNum);
}

/**
 * Poner en cola la renderización de una página
 * @param {number} num - Número de página a renderizar
 */
function queueRenderPage(num) {
    if (pageRendering) {
        pageNumPending = num;
    } else {
        renderPage(num);
    }
}

/**
 * Inicia el seguimiento de la sesión de lectura
 */
function startTracking() {
    startTime = new Date();
    sesionActiva = true;

    // Iniciar medición de caracteres por minuto basado en los cambios en el textarea
    const notesTextarea = document.getElementById('userNotes');
    let lastLength = notesTextarea.value.length;

    // Verificar cambios cada segundo
    setInterval(() => {
        if (!sesionActiva) return;

        const currentLength = notesTextarea.value.length;
        const newChars = Math.max(0, currentLength - lastLength);
        caracteresProcesados += newChars;
        lastLength = currentLength;

        // Actualizar CPM (Caracteres Por Minuto)
        updateCPM();
    }, 1000);

    // Guardar progreso automáticamente cada 2 minutos
    setInterval(() => {
        if (sesionActiva) {
            saveProgress(true); // true indica que es guardado automático
        }
    }, 120000); // 2 minutos
}

/**
 * Actualiza la visualización de Caracteres Por Minuto
 */
function updateCPM() {
    const elapsedMinutes = (new Date() - startTime) / 60000; // Convertir a minutos
    if (elapsedMinutes > 0) {
        const cpm = Math.round(caracteresProcesados / elapsedMinutes);
        document.getElementById('caracteresMinuto').textContent = cpm;
    }
}

/**
 * Actualiza el porcentaje de progreso
 */
function updateProgress() {
    // Calcular el progreso en porcentaje (basado en páginas y texto)
    const paginasCompletadas = pageNum - 1;
    const porcentajePaginas = (paginasCompletadas / totalPages) * 100;

    // Combinar con el progreso de texto ingresado (peso 70% páginas, 30% texto)
    const notesLength = document.getElementById('userNotes').value.length;
    const porcentajeTexto = Math.min(100, notesLength / 300 * 100); // Asumir 300 caracteres como meta

    const porcentajeTotal = (porcentajePaginas * 0.7) + (porcentajeTexto * 0.3);

    // Actualizar progreso visual
    document.getElementById('progresoLectura').textContent = `${Math.round(porcentajeTotal)}%`;

    // Actualizar en el objeto de progreso
    progresoActual.porcentajeCompletado = porcentajeTotal;
    progresoActual.paginaActual = pageNum;
}

/**
 * Guarda el progreso actual
 * @param {boolean} autoSave - Indica si es guardado automático
 */
function saveProgress(autoSave = false) {
    // Actualizar datos del progreso
    progresoActual.paginaActual = pageNum;
    progresoActual.textoCompletado = document.getElementById('userNotes').value;

    // Crear formData para enviar al servidor
    const formData = new FormData();
    formData.append('documentoId', documentoId);
    formData.append('paginaActual', progresoActual.paginaActual);
    formData.append('indiceCaracter', progresoActual.indiceCaracter);
    formData.append('posicionX', progresoActual.posicionX);
    formData.append('posicionY', progresoActual.posicionY);
    formData.append('elementoId', progresoActual.elementoId);
    formData.append('fragmentoContexto', progresoActual.fragmentoContexto);
    formData.append('porcentajeCompletado', progresoActual.porcentajeCompletado);
    formData.append('textoCompletado', progresoActual.textoCompletado);

    // Añadir token anti-forgery
    if (antiForgeryToken) {
        formData.append('__RequestVerificationToken', antiForgeryToken);
    }

    // Enviar datos al servidor
    fetch('/PDFAdmin/GuardarProgreso', {
        method: 'POST',
        body: formData
    })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                // Actualizar indicadores
                const estadoGuardado = document.getElementById('estadoGuardado');
                estadoGuardado.textContent = 'Guardado';
                estadoGuardado.className = 'badge badge-primary';

                const ultimaActualizacion = document.getElementById('ultimaActualizacion');
                ultimaActualizacion.textContent = 'Última actualización: ahora';

                if (!autoSave) {
                    // Mostrar una mini notificación solo para guardado manual
                    showNotification('Progreso guardado correctamente', 'success');
                }
            } else {
                // Marcar error en los indicadores
                const estadoGuardado = document.getElementById('estadoGuardado');
                estadoGuardado.textContent = 'Error al guardar';
                estadoGuardado.className = 'badge badge-danger';

                if (!autoSave) {
                    showNotification('Error al guardar el progreso', 'danger');
                }
            }
        })
        .catch(error => {
            console.error('Error al guardar el progreso:', error);
            const estadoGuardado = document.getElementById('estadoGuardado');
            estadoGuardado.textContent = 'Error';
            estadoGuardado.className = 'badge badge-danger';

            if (!autoSave) {
                showNotification('Error al guardar el progreso', 'danger');
            }
        });
}

/**
 * Muestra una notificación temporal
 * @param {string} message - Mensaje a mostrar
 * @param {string} type - Tipo de notificación (success, warning, danger, info)
 */
function showNotification(message, type) {
    // Crear elemento de notificación
    const notification = document.createElement('div');
    notification.className = `alert alert-${type}`;
    notification.style.position = 'fixed';
    notification.style.top = '20px';
    notification.style.right = '20px';
    notification.style.padding = 'var(--spacing-md)';
    notification.style.borderRadius = 'var(--border-radius-sm)';
    notification.style.zIndex = '9999';
    notification.style.boxShadow = 'var(--shadow-md)';
    notification.style.opacity = '0';
    notification.style.transition = 'opacity 0.3s ease-in-out';
    notification.textContent = message;

    // Añadir al DOM
    document.body.appendChild(notification);

    // Mostrar con animación
    setTimeout(() => {
        notification.style.opacity = '1';
    }, 10);

    // Ocultar después de 3 segundos
    setTimeout(() => {
        notification.style.opacity = '0';
        setTimeout(() => {
            document.body.removeChild(notification);
        }, 300);
    }, 3000);
}

/**
 * Finaliza la sesión y guarda estadísticas
 */
function finishSession() {
    if (!sesionActiva) return;

    // Calcular duración y WPM
    const endTime = new Date();
    const duracionMinutos = (endTime - startTime) / 60000; // Convertir a minutos
    const wpm = Math.round(caracteresProcesados / 5 / duracionMinutos); // 5 caracteres = 1 palabra (promedio)

    // Crear formData para enviar estadísticas
    const formData = new FormData();
    formData.append('documentoId', documentoId);
    formData.append('duracionMinutos', duracionMinutos);
    formData.append('wpm', wpm);
    formData.append('paginaInicio', progresoActual.paginaActual);
    formData.append('paginaFin', pageNum);

    // Añadir token anti-forgery
    if (antiForgeryToken) {
        formData.append('__RequestVerificationToken', antiForgeryToken);
    }

    // Enviar estadísticas al servidor
    fetch('/PDFAdmin/GuardarEstadisticas', {
        method: 'POST',
        body: formData
    })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                console.log('Estadísticas guardadas correctamente');
            } else {
                console.error('Error al guardar estadísticas:', data.message);
            }
        })
        .catch(error => {
            console.error('Error al guardar estadísticas:', error);
        });

    // Marcar sesión como finalizada
    sesionActiva = false;
}

// Registrar el evento beforeunload para guardar al salir de la página
window.addEventListener('beforeunload', function (e) {
    // Guardar progreso antes de salir
    if (sesionActiva) {
        saveProgress(true);
        finishSession();
    }
});

// Exponer la función initPdfViewer globalmente
window.initPdfViewer = initPdfViewer;