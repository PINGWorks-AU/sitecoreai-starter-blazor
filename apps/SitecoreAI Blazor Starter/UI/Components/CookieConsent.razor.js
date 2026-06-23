globalThis.updateConsent = function() {
	const pnl = document.getElementById("consent-panel");
	const chkEvt = document.getElementById("events-enabled");
	const chkPrf = document.getElementById("profile-enabled");

	if (pnl && pnl.style.display !== "none") {
		// persist the choice immediately, then animate the panel out
		setCookie("pw#EventConsent", chkEvt.checked ? "1" : "0", 7);
		setCookie("pw#ProfileConsent", chkPrf.checked ? "1" : "0", 7);

		fetch(`/api/token?path=${encodeURIComponent(location.pathname + location.search)}&referrer=${encodeURIComponent(document.referrer)}`)
			.then(resp => resp.json())
			.then(obj => setCookie(obj.name, obj.browserContextId, obj.expiryDays))
			.catch();

		// trigger the exit animation; honour reduced-motion users
		const reducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
		if (reducedMotion) {
			pnl.style.display = "none";
		} else {
			pnl.classList.add("is-leaving");
			pnl.addEventListener("animationend", () => { pnl.style.display = "none"; }, { once: true });
		}
	}
}

function setCookie(name, value, expiryDays) {
	cookieStore.set({
		name: name,
		value: value,
		path: '/',
		sameSite: "none",
		expires: Date.now() + (expiryDays * 86400 * 1000)
	});
}
