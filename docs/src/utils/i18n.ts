import i18n from "i18next";
import Backend from "i18next-http-backend";
import LanguageDetector from "i18next-browser-languagedetector";
import { initReactI18next } from "react-i18next";

let translationFilePath = "/locales/{{lng}}.json";
if (import.meta.env.DEV) {
	translationFilePath = "/locales/base.json";
}

void i18n
	.use(Backend)
	.use(LanguageDetector)
	.use(initReactI18next)
	.init({
		debug: false,
		supportedLngs: ["en-US"],
		fallbackLng: "en-US",
		interpolation: {
			escapeValue: false,
		},
		backend: {
			loadPath: translationFilePath,
		},
		react: {
			useSuspense: true,
		},
	});

export default i18n;
