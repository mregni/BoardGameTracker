import i18n from 'i18next';
import LanguageDetector from 'i18next-browser-languagedetector';
import Backend from 'i18next-http-backend';
import {initReactI18next} from 'react-i18next';

console.log(process.env.NODE_ENV)
let translationFilePath = '/locales/{{lng}}.json';
if (process.env.NODE_ENV === 'development') {
  translationFilePath = '/locales/base.json';
}

void i18n
  .use(Backend)
  .use(LanguageDetector)
  .use(initReactI18next)
  .init({
    debug: true,
    fallbackLng: 'en',
    interpolation: {
      escapeValue: false,
    },
    backend: {
      loadPath: translationFilePath,
    },
    react: {
      useSuspense: true,
    }
  });

export default i18n;