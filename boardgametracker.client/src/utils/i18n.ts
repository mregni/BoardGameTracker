import i18n from "i18next";
import LanguageDetector from "i18next-browser-languagedetector";
import Backend from "i18next-http-backend";
import { initReactI18next } from "react-i18next";

const namespaces = [
	"version",
	"game-nights",
	"common",
	"shames",
	"loans",
	"dashboard",
	"expansions",
	"bgg-import",
	"compare",
	"game",
	"games",
	"images",
	"player",
	"error",
	"not-found",
	"location",
	"statistics",
	"sessions",
	"settings",
	"player-session",
	"language",
	"log-levels",
	"badges",
	"rsvp",
	"auth",
] as const;

let translationFilePath = "/locales/{{lng}}/{{ns}}.json";
if (import.meta.env.DEV) {
	translationFilePath = "/locales/base/{{ns}}.json";
}

void i18n
	.use(Backend)
	.use(LanguageDetector)
	.use(initReactI18next)
	.init({
		debug: false,
		supportedLngs: ["en-US", "nl-NL", "nl-BE"],
		fallbackLng: "en-US",
		ns: [...namespaces],
		defaultNS: "common",
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

i18n.services.formatter?.add(
	"capitalize",
	(value: string) => `${value.substring(0, 1).toUpperCase()}${value.substring(1)}`,
);

export default i18n;
