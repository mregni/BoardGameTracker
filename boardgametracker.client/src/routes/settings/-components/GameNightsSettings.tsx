import { useTranslation } from 'react-i18next';

import { SettingsToggle } from './SettingsToggle';
import { SettingsSection } from './SettingsSection';

import { SettingsSchema } from '@/models';
import { BgtFormField, BgtInputField } from '@/components/BgtForm';

interface Props {
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  form: any;
  disabled?: boolean;
}

export const GameNightsSettings = ({ form, disabled = false }: Props) => {
  const { t } = useTranslation();

  return (
    <div className="space-y-6">
      <SettingsSection title={t('settings.game-nights.title')} description={t('settings.game-nights.description')}>
        <BgtFormField form={form} name="gameNightsEnabled" schema={SettingsSchema}>
          {(field) => (
            <SettingsToggle
              field={field}
              label={t('settings.game-nights.enabled.label')}
              description={t('settings.game-nights.enabled.description')}
              disabled={disabled}
            />
          )}
        </BgtFormField>

        <form.Subscribe
          selector={(state: { values: { gameNightsEnabled: boolean } }) => state.values.gameNightsEnabled}
        >
          {(gameNightsEnabled: boolean) =>
            gameNightsEnabled && (
              <BgtFormField form={form} name="publicUrl" schema={SettingsSchema}>
                {(field) => (
                  <BgtInputField
                    field={field}
                    disabled={disabled}
                    type="text"
                    label={t('settings.game-nights.public-url.label')}
                    placeholder={t('settings.game-nights.public-url.placeholder')}
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
