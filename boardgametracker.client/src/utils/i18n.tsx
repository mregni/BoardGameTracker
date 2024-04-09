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
    debug: false,
    supportedLngs: [
      'en-US',
      'nl-NL',
    ],
    fallbackLng: 'en-US',
    interpolation: {
      escapeValue: false,
      format: (value: string, format) => {
        if (format === 'capitalize') return `${value.substring(0, 1).toUpperCase()}${value.substring(1)}`;
        return value;
      },
    },
    backend: {
      loadPath: translationFilePath,
    },
    react: {
      useSuspense: true,
    }
  });

export default i18n;