/**
 * Archivo de manejo de PDFs para el módulo de usuario
 * Este script maneja la zona de arrastre de archivos y la eliminación de PDFs
 */

document.addEventListener('DOMContentLoaded', function () {
    // Elementos del DOM
    const dropZone = document.getElementById('dropZone');
    const fileInput = document.getElementById('pdfFiles');
    const selectFilesBtn = document.getElementById('selectFilesBtn');
    const fileList = document.getElementById('fileList');
    const selectedFilesList = document.getElementById('selectedFiles');
    const deleteButtons = document.querySelectorAll('.delete-pdf');
    const deleteModal = document.getElementById('deleteModal');
    const pdfNameElement = document.getElementById('pdfName');
    const pdfIdInput = document.getElementById('pdfId');

    // Si el dropzone no está presente, no continuamos
    if (!dropZone) return;

    // Evento para seleccionar archivos al hacer clic en el botón
    selectFilesBtn.addEventListener('click', () => {
        fileInput.click();
    });

    // Manejar eventos de arrastrar y soltar
    ['dragenter', 'dragover', 'dragleave', 'drop'].forEach(eventName => {
        dropZone.addEventListener(eventName, preventDefaults, false);
    });

    function preventDefaults(e) {
        e.preventDefault();
        e.stopPropagation();
    }

    // Cambiar estilo cuando se arrastra un archivo sobre la zona
    ['dragenter', 'dragover'].forEach(eventName => {
        dropZone.addEventListener(eventName, highlight, false);
    });

    ['dragleave', 'drop'].forEach(eventName => {
        dropZone.addEventListener(eventName, unhighlight, false);
    });

    function highlight() {
        dropZone.style.borderColor = 'var(--primary-color)';
        dropZone.style.backgroundColor = 'rgba(52, 152, 219, 0.1)';
    }

    function unhighlight() {
        dropZone.style.borderColor = 'var(--neutral-600)';
        dropZone.style.backgroundColor = 'var(--neutral-700)';
    }

    // Manejar cuando se sueltan archivos
    dropZone.addEventListener('drop', handleDrop, false);

    function handleDrop(e) {
        const dt = e.dataTransfer;
        const files = dt.files;
        handleFiles(files);
    }

    // Manejar cuando se seleccionan archivos desde el input
    fileInput.addEventListener('change', function () {
        handleFiles(this.files);
    });

    // Procesar los archivos seleccionados
    function handleFiles(files) {
        if (files.length > 0) {
            // Mostrar la lista de archivos seleccionados
            fileList.style.display = 'block';
            selectedFilesList.innerHTML = '';

            // Validar que solo sean archivos PDF
            let allValid = true;
            Array.from(files).forEach(file => {
                const fileExt = file.name.split('.').pop().toLowerCase();
                const isValidPDF = fileExt === 'pdf';
                const isValidSize = file.size <= 25 * 1024 * 1024; // 25MB max

                const li = document.createElement('li');

                if (!isValidPDF) {
                    li.innerHTML = `<span style="color: var(--danger-color);">${file.name} - No es un archivo PDF válido</span>`;
                    allValid = false;
                } else if (!isValidSize) {
                    li.innerHTML = `<span style="color: var(--danger-color);">${file.name} - Excede el tamaño máximo de 25MB</span>`;
                    allValid = false;
                } else {
                    const fileSizeMB = (file.size / (1024 * 1024)).toFixed(2);
                    li.innerHTML = `${file.name} - ${fileSizeMB} MB`;
                }

                selectedFilesList.appendChild(li);
            });

            // Habilitar o deshabilitar el botón de subida según validación
            const submitBtn = document.querySelector('#fileList button[type="submit"]');
            submitBtn.disabled = !allValid;
            if (!allValid) {
                submitBtn.style.opacity = '0.6';
                submitBtn.style.cursor = 'not-allowed';
            } else {
                submitBtn.style.opacity = '1';
                submitBtn.style.cursor = 'pointer';
            }
        }
    }

    // Configurar la funcionalidad de eliminación de PDFs
    deleteButtons.forEach(button => {
        button.addEventListener('click', function () {
            const pdfId = this.getAttribute('data-id');
            const pdfName = this.getAttribute('data-name');

            // Establecer los valores en el modal
            pdfNameElement.textContent = pdfName;
            pdfIdInput.value = pdfId;

            // Mostrar el modal usando Bootstrap
            $('#deleteModal').modal('show');
        });
    });
});