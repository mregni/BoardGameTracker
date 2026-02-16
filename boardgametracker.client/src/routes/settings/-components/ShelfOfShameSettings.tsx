import { useTranslation } from 'react-i18next';
import type { AnyFieldApi } from '@tanstack/react-form';

import { settingsFormOpts } from '../-utils/settingsFormOpts';

import { SettingsToggle } from './SettingsToggle';
import { SettingsSection } from './SettingsSection';

import { zodValidator } from '@/utils/zodValidator';
import { SettingsSchema } from '@/models';
import { withForm } from '@/hooks/form';
import { BgtInputField } from '@/components/BgtForm';

export const ShelfOfShameSettings = withForm({
  ...settingsFormOpts,
  props: {
    disabled: false,
  },
  render: function Render({ form, disabled }) {
    const { t } = useTranslation();

    return (
      <div className="space-y-6">
        <SettingsSection title={t('settings.shame.title')} description={t('settings.shame.description')}>
          <form.Field name="shelfOfShameEnabled" validators={zodValidator(SettingsSchema, 'shelfOfShameEnabled')}>
            {(field: AnyFieldApi) => (
              <SettingsToggle
                field={field}
                label={t('settings.shame.enabled.label')}
                description={t('settings.shame.enabled.description')}
                disabled={disabled}
              />
            )}
          </form.Field>

          <form.Subscribe
            selector={(state: { values: { shelfOfShameEnabled: boolean } }) => state.values.shelfOfShameEnabled}
          >
            {(shelfOfShameEnabled: boolean) =>
              shelfOfShameEnabled && (
                <form.Field
                  name="shelfOfShameMonthsLimit"
                  validators={zodValidator(SettingsSchema, 'shelfOfShameMonthsLimit')}
                >
                  {(field: AnyFieldApi) => (
                    <BgtInputField
                      field={field}
                      disabled={disabled}
                      type="number"
                      label={t('settings.shame.months.label')}
                      placeholder="3"
                    />
                  )}
                </form.Field>
              )
            }
          </form.Subscribe>
        </SettingsSection>
      </div>
    );
  },
});
