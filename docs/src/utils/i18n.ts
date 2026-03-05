import i18n from "i18next";
import Backend from "i18next-http-backend";
import LanguageDetector from "i18next-browser-languagedetector";
import { initReactI18next } from "react-i18next";

const base = import.meta.env.BASE_URL;

void i18n
	.use(Backend)
	.use(LanguageDetector)
	.use(initReactI18next)
	.init({
		debug: false,
		supportedLngs: ["en-US"],
		fallbackLng: "en-US",
		ns: ["common", "home", "getting-started", "extra", "sidebar"],
		defaultNS: "common",
		interpolation: {
			escapeValue: false,
		},
		backend: {
			loadPath: `${base}locales/{{lng}}/{{ns}}.json`,
		},
		react: {
			useSuspense: true,
		},
	});

export default i18n;
