window.preventDefaultEventOnEnterKeyPress = (id) => {
    const element = document.getElementById(id);

    if (!element) return;

    element.addEventListener("keydown", (event) => {
        if (event.key === "Enter") {
            event.preventDefault();
            return false;
        }
    });
}

window.setNumericInputValue = (arg, value) => {
    const ref = arg instanceof Element ? arg : document.getElementById(arg);

    if (ref) {
        const input = ref.getElementsByTagName("input")[0]
        if (!input.value.toString().endsWith('.')) {
            input.value = value;
        }
    }
}

window.setMarkerAttributes = (arg, itemsToMark, parentSelector) => {
    const el = arg instanceof Element ? arg : document.getElementById(arg);
    if (!el) return;

    for (const [selector, attributes] of Object.entries(itemsToMark)) {
        let itemEl = el.querySelector(selector);
        if (!itemEl) continue;

        if (parentSelector) {
            itemEl = itemEl.closest(parentSelector);
        }
        if (!itemEl) continue;
        
        for (const [attribute, value] of Object.entries(attributes)) {
            if (!value) {
                itemEl.removeAttribute(attribute);
            } else {
                itemEl.setAttribute(attribute, value);
            }
        }
    }

}


window.__trident__maskedInputs = {}
window.setInputMask = (uniqueId, arg, mask) => {
    const input = arg instanceof Element ? arg : document.getElementById(arg);
    if (window.__trident__maskedInputs[uniqueId]) {
        window.__trident__maskedInputs[uniqueId].updateOptions({mask: mask})
        window.updateInputMask(uniqueId);
    } else {
        window.__trident__maskedInputs[uniqueId] = IMask(input, {
            mask: mask
        });
    }
}

window.updateInputMask = (uniqueId) => {
    window.__trident__maskedInputs[uniqueId].updateValue();
}

window.disposeInputMask = (uniqueId) => {
    delete window.__trident__maskedInputs[uniqueId];
}
