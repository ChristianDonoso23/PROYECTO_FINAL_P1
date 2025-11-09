document.addEventListener("DOMContentLoaded", () => {
    const form = document.querySelector("form");
    if (!form) return;

    form.addEventListener("submit", (e) => {
        limpiarErrores();
        let errores = [];

        const cedula = obtenerValor("Cedula");
        const nombre = obtenerValor("Nombre");
        const edad = parseInt(obtenerValor("Edad"));
        const estatura = parseInt(obtenerValor("Estatura"));
        const peso = parseFloat(obtenerValor("Peso"));
        const genero = obtenerValor("Genero");

        // Validar cédula (solo si tiene contenido)
        if (cedula && !validarCedulaEcuatoriana(cedula)) {
            mostrarError("Cedula", "⚠️ Cédula ecuatoriana inválida.");
            errores.push("Cédula inválida");
        }

        // Validar nombre (solo si tiene contenido)
        if (nombre && !/^[A-Za-zÁÉÍÓÚáéíóúÑñ\s]+$/.test(nombre)) {
            mostrarError("Nombre", "⚠️ El nombre solo puede contener letras.");
            errores.push("Nombre inválido");
        }

        // Validar edad
        if (edad && (isNaN(edad) || edad < 1 || edad > 120)) {
            mostrarError("Edad", "⚠️ Edad fuera de rango (1–120).");
            errores.push("Edad inválida");
        }

        // Validar estatura
        if (estatura && (isNaN(estatura) || estatura < 30 || estatura > 250)) {
            mostrarError("Estatura", "⚠️ Estatura fuera de rango (30–250 cm).");
            errores.push("Estatura inválida");
        }

        // Validar peso
        if (peso && (isNaN(peso) || peso < 1 || peso > 300)) {
            mostrarError("Peso", "⚠️ Peso fuera de rango (1–300 kg).");
            errores.push("Peso inválido");
        }

        // Validar género
        if (genero && !["M", "F", "O"].includes(genero.toUpperCase())) {
            mostrarError("Genero", "⚠️ Género inválido (M, F u O).");
            errores.push("Género inválido");
        }

        if (errores.length > 0) {
            e.preventDefault();
            e.stopImmediatePropagation();
            mostrarResumenErrores(errores);
        }
    });
});

function obtenerValor(nombreCampo) {
    const input = document.querySelector(`[name='${nombreCampo}']`);
    return input ? input.value.trim() : "";
}

function mostrarError(campo, mensaje) {
    const input = document.querySelector(`[name='${campo}']`);
    if (!input) return;

    const span = document.createElement("span");
    span.classList.add("text-danger", "fw-bold", "d-block", "mt-1");
    span.textContent = mensaje;
    input.insertAdjacentElement("afterend", span);

    input.classList.add("border-danger");
    input.style.borderColor = "#dc3545";
    input.style.borderWidth = "2px";
}

function limpiarErrores() {
    document.querySelectorAll(".text-danger:not(.field-validation-error)").forEach(e => e.remove());
    document.querySelectorAll("input, select").forEach(i => {
        i.classList.remove("border-danger");
        i.style.borderColor = "";
        i.style.borderWidth = "";
    });
    document.querySelectorAll(".alert-danger").forEach(e => e.remove());
}

function mostrarResumenErrores(errores) {
    const form = document.querySelector("form");
    const alert = document.createElement("div");
    alert.className = "alert alert-danger mt-3";
    alert.innerHTML = `
        <strong>⚠️ Corrija los siguientes errores:</strong>
        <ul class="mb-0">${errores.map(e => `<li>${e}</li>`).join("")}</ul>
    `;
    form.insertAdjacentElement("afterbegin", alert);
}

// ========================================
// ALGORITMO CORRECTO DE VALIDACIÓN DE CÉDULA ECUATORIANA
// ========================================
function validarCedulaEcuatoriana(cedula) {
    // Verificar que tenga 10 dígitos
    if (!cedula || cedula.length !== 10 || !/^\d{10}$/.test(cedula)) {
        return false;
    }

    // Extraer código de provincia (primeros 2 dígitos)
    const provincia = parseInt(cedula.substring(0, 2));

    // Verificar que sea una provincia válida (01-24)
    if (provincia < 1 || provincia > 24) {
        console.log("❌ Provincia inválida:", provincia);
        return false;
    }

    // Verificar el tercer dígito (debe ser menor a 6)
    const tercerDigito = parseInt(cedula.charAt(2));
    if (tercerDigito > 5) {
        console.log("❌ Tercer dígito inválido:", tercerDigito);
        return false;
    }

    // Coeficientes oficiales del algoritmo ecuatoriano
    const coeficientes = [2, 1, 2, 1, 2, 1, 2, 1, 2];

    let suma = 0;

    // Aplicar el algoritmo de módulo 10
    for (let i = 0; i < 9; i++) {
        let digito = parseInt(cedula.charAt(i));
        let producto = digito * coeficientes[i];

        // Si el producto es mayor o igual a 10, restar 9
        if (producto >= 10) {
            producto -= 9;
        }

        suma += producto;
    }

    // Calcular el dígito verificador esperado
    const digitoVerificadorEsperado = (10 - (suma % 10)) % 10;

    // Obtener el dígito verificador real (último dígito)
    const digitoVerificadorReal = parseInt(cedula.charAt(9));

    // Comparar
    const esValida = digitoVerificadorEsperado === digitoVerificadorReal;

    if (!esValida) {
        console.log("❌ Cédula inválida:", cedula);
        console.log("   Dígito esperado:", digitoVerificadorEsperado);
        console.log("   Dígito recibido:", digitoVerificadorReal);
    } else {
        console.log("✅ Cédula válida:", cedula);
    }

    return esValida;
}