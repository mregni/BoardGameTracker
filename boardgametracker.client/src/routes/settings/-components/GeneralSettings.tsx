import { useTranslation } from 'react-i18next';

import { SettingsSection } from './SettingsSection';

import { SettingsSchema } from '@/models';
import { BgtFormField, BgtInputField, BgtSelect } from '@/components/BgtForm';

interface Props {
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  form: any;
  languages: { key: string; translationKey: string }[];
  disabled?: boolean;
}

export const GeneralSettings = ({ form, languages, disabled = false }: Props) => {
  const { t } = useTranslation();

  return (
    <>
      <SettingsSection
        title={t('settings.general.language.title')}
        description={t('settings.general.language.description')}
      >
        <BgtFormField form={form} name="uiLanguage" schema={SettingsSchema.shape.uiLanguage}>
          {(field) => (
            <BgtSelect
              field={field}
              disabled={disabled}
              label={t('settings.general.language.label')}
              items={languages.map((value) => ({
                label: t(`languages.${value.translationKey}`),
                value: value.key,
              }))}
            />
          )}
        </BgtFormField>
      </SettingsSection>

      <SettingsSection
        title={t('settings.general.date-time.title')}
        description={t('settings.general.date-time.description')}
      >
        <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
          <BgtFormField form={form} name="dateFormat" schema={SettingsSchema.shape.dateFormat}>
            {(field) => (
              <BgtInputField
                field={field}
                disabled={disabled}
                type="text"
                label={t('settings.general.date-time.date-format')}
                placeholder="MM/DD/YYYY"
              />
            )}
          </BgtFormField>
          <BgtFormField form={form} name="timeFormat" schema={SettingsSchema.shape.timeFormat}>
            {(field) => (
              <BgtInputField
                field={field}
                disabled={disabled}
                type="text"
                label={t('settings.general.date-time.time-format')}
                placeholder="HH:mm"
              />
            )}
          </BgtFormField>
        </div>
      </SettingsSection>

      <SettingsSection
        title={t('settings.general.currency.title')}
        description={t('settings.general.currency.description')}
      >
        <BgtFormField form={form} name="currency" schema={SettingsSchema.shape.currency}>
          {(field) => (
            <BgtInputField
              field={field}
              disabled={disabled}
              type="text"
              label={t('settings.general.currency.label')}
              placeholder="USD"
            />
          )}
        </BgtFormField>
      </SettingsSection>
    </>
  );
};
