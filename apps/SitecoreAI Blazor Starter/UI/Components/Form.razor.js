export function onUpdate(id) {
    if (typeof grecaptcha === 'undefined') {
        reloadFormScript(id);
    } else {
        try {
            grecaptcha.reset();
        }
        catch {
            reloadFormScript(id);
        }
    }
}

function reloadFormScript(id) {

    const component = document.getElementById(id);
    const form = component.querySelector("& > div.main-form-wrapper");
    const scrOrig = form.querySelector("& > script");
    const scrNew = document.createElement("script");
    const button = form.querySelectorAll('.submit-button');

    if (button) {
        button.onclick = null;
    }

    // duplicate properties and content
    Array.from(scrOrig.attributes).forEach(a => { scrNew.setAttribute(a.name, a.value); });
    scrNew.text = scrOrig.textContent.replace(
        'document.currentScript.parentNode',
        `document.getElementById(${JSON.stringify(id)})`
    );

    // re-add the element to the dom
    form.appendChild(scrNew);
    scrOrig.remove();
}
