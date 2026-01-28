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
      <SettingsSection title={t('settings.general.title')} description={t('settings.general.description')}>
        <BgtFormField form={form} name="publicUrl" schema={SettingsSchema.shape.publicUrl}>
          {(field) => (
            <BgtInputField
              field={field}
              disabled={disabled}
              type="text"
              label={t('settings.general.public-url.label')}
              placeholder={t('settings.general.public-url.placeholder')}
            />
          )}
        </BgtFormField>
      </SettingsSection>

      <SettingsSection title="Language Settings" description="Set the language for your application.">
        <BgtFormField form={form} name="uiLanguage" schema={SettingsSchema.shape.uiLanguage}>
          {(field) => (
            <BgtSelect
              field={field}
              disabled={disabled}
              label="Language"
              items={languages.map((value) => ({
                label: value.translationKey,
                value: value.key,
              }))}
            />
          )}
        </BgtFormField>
      </SettingsSection>

      <SettingsSection
        title="Date and Time Settings"
        description="Configure date and time formats to match your preferences."
      >
        <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
          <BgtFormField form={form} name="dateFormat" schema={SettingsSchema.shape.dateFormat}>
            {(field) => (
              <BgtInputField
                field={field}
                disabled={disabled}
                type="text"
                label="Date Format"
                placeholder="MM/DD/YYYY"
              />
            )}
          </BgtFormField>
          <BgtFormField form={form} name="timeFormat" schema={SettingsSchema.shape.timeFormat}>
            {(field) => (
              <BgtInputField field={field} disabled={disabled} type="text" label="Time Format" placeholder="HH:mm" />
            )}
          </BgtFormField>
        </div>
      </SettingsSection>

      <SettingsSection title="Currency Settings" description="Set the currency for financial transactions.">
        <BgtFormField form={form} name="currency" schema={SettingsSchema.shape.currency}>
          {(field) => (
            <BgtInputField field={field} disabled={disabled} type="text" label="Currency" placeholder="USD" />
          )}
        </BgtFormField>
      </SettingsSection>
    </>
  );
};
