import { useTranslation } from 'react-i18next';
import { cx } from 'class-variance-authority';

import { SettingsToggle } from './SettingsToggle';
import { SettingsSection } from './SettingsSection';

import { SettingsSchema } from '@/models';
import { BgtText } from '@/components/BgtText/BgtText';
import { BgtFormField } from '@/components/BgtForm';
import BgtButton from '@/components/BgtButton/BgtButton';
import GitHubIcon from '@/assets/icons/github.svg?react';
import CrowdinIcon from '@/assets/icons/crowdin.svg?react';

interface Props {
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  form: any;
  disabled?: boolean;
}

const VERSION_TRACKS = [
  {
    id: 'stable',
    label: 'settings.advanced.version-track.stable.label',
    description: 'settings.advanced.version-track.stable.description',
  },
  {
    id: 'beta',
    label: 'settings.advanced.version-track.beta.label',
    description: 'settings.advanced.version-track.beta.description',
  },
];

export const AdvancedSettings = ({ form, disabled = false }: Props) => {
  const { t } = useTranslation();

  return (
    <div className="space-y-6">
      <SettingsSection
        title={t('settings.advanced.updates.title')}
        description={t('settings.advanced.updates.description')}
      >
        <BgtFormField form={form} name="updateCheckEnabled" schema={SettingsSchema}>
          {(field) => (
            <SettingsToggle
              field={field}
              label={t('settings.advanced.updates.enabled.label')}
              description={t('settings.advanced.updates.enabled.description')}
              disabled={disabled}
            />
          )}
        </BgtFormField>

        <BgtFormField form={form} name="versionTrack" schema={SettingsSchema}>
          {(field) => (
            <div className="flex flex-col gap-1">
              <BgtText size="2" weight="medium" color="white">
                {t('settings.advanced.version-track.label')}
              </BgtText>
              <div className="grid grid-cols-1 md:grid-cols-3 gap-2">
                {VERSION_TRACKS.map((track) => (
                  <button
                    key={track.id}
                    type="button"
                    onClick={() => field.handleChange(track.id)}
                    disabled={disabled}
                    className={cx(
                      'p-3 rounded-lg border transition-all text-left',
                      field.state.value === track.id
                        ? 'bg-primary/20 border-primary shadow-lg shadow-primary/20'
                        : 'bg-background border-white/10 hover:border-white/20'
                    )}
                  >
                    <div className="flex items-center gap-2 mb-1">
                      <span
                        className={cx(
                          'text-sm font-medium',
                          field.state.value === track.id ? 'text-white' : 'text-gray-300'
                        )}
                      >
                        {t(track.label)}
                      </span>
                    </div>
                    <BgtText size="1" color="white" opacity={50}>
                      {t(track.description)}
                    </BgtText>
                  </button>
                ))}
              </div>
            </div>
          )}
        </BgtFormField>
      </SettingsSection>

      <SettingsSection
        title={t('settings.advanced.statistics.title')}
        description={t('settings.advanced.statistics.description')}
      >
        <BgtFormField form={form} name="statistics" schema={SettingsSchema}>
          {(field) => (
            <SettingsToggle
              field={field}
              label={t('settings.advanced.statistics.enabled.label')}
              description={t('settings.advanced.statistics.enabled.description')}
              disabled={disabled}
            />
          )}
        </BgtFormField>
      </SettingsSection>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
        <BgtButton onClick={() => window.open('https://github.com/mregni/BoardGameTracker/issues')} variant="primary">
          <GitHubIcon className="size-4" />
          {t('settings.advanced.bug')}
        </BgtButton>
        <BgtButton onClick={() => window.open('https://crowdin.com/project/boardgametracker')} variant="primary">
          <CrowdinIcon className="size-4" />
          {t('settings.advanced.translate')}
        </BgtButton>
      </div>
    </div>
  );
};
