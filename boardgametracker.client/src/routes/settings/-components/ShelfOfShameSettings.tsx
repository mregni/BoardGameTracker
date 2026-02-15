import { useTranslation } from 'react-i18next';

import { SettingsToggle } from './SettingsToggle';
import { SettingsSection } from './SettingsSection';

import { SettingsSchema } from '@/models';
import { type AnyReactForm, BgtFormField, BgtInputField } from '@/components/BgtForm';

interface Props {
  form: AnyReactForm;
  disabled?: boolean;
}

export const ShelfOfShameSettings = ({ form, disabled = false }: Props) => {
  const { t } = useTranslation();

  return (
    <div className="space-y-6">
      <SettingsSection title={t('settings.shame.title')} description={t('settings.shame.description')}>
        <BgtFormField form={form} name="shelfOfShameEnabled" schema={SettingsSchema}>
          {(field) => (
            <SettingsToggle
              field={field}
              label={t('settings.shame.enabled.label')}
              description={t('settings.shame.enabled.description')}
              disabled={disabled}
            />
          )}
        </BgtFormField>

        <form.Subscribe
          selector={(state: { values: { shelfOfShameEnabled: boolean } }) => state.values.shelfOfShameEnabled}
        >
          {(shelfOfShameEnabled: boolean) =>
            shelfOfShameEnabled && (
              <BgtFormField
                form={form}
                name="shelfOfShameMonthsLimit"
                schema={SettingsSchema}
              >
                {(field) => (
                  <BgtInputField
                    field={field}
                    disabled={disabled}
                    type="number"
                    label={t('settings.shame.months.label')}
                    placeholder="3"
                  />
                )}
              </BgtFormField>
            )
          }
        </form.Subscribe>
      </SettingsSection>
    </div>
  );
};
