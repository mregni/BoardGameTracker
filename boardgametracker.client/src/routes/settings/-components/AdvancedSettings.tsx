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
  { id: 'stable', label: 'Stable', description: 'Recommended for production use.' },
  { id: 'beta', label: 'Beta', description: 'Early access to new features.' },
];

export const AdvancedSettings = ({ form, disabled = false }: Props) => {
  return (
    <div className="space-y-6">
      <SettingsSection title="Update Check Settings" description="Configure how the application checks for updates.">
        <BgtFormField form={form} name="updateCheckEnabled" schema={SettingsSchema.shape.updateCheckEnabled}>
          {(field) => (
            <SettingsToggle
              field={field}
              label="Enable Update Check"
              description="Automatically check for new versions when the app starts"
              disabled={disabled}
            />
          )}
        </BgtFormField>

        <BgtFormField form={form} name="versionTrack" schema={SettingsSchema.shape.versionTrack}>
          {(field) => (
            <div>
              <BgtText size="2" weight="medium" color="white" className="mb-2">
                Version Track
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
                        {track.label}
                      </span>
                    </div>
                    <BgtText size="1" color="white" opacity={50}>
                      {track.description}
                    </BgtText>
                  </button>
                ))}
              </div>
            </div>
          )}
        </BgtFormField>
      </SettingsSection>

      <SettingsSection title="Statistics" description="Configure statistics collection.">
        <BgtFormField form={form} name="statistics" schema={SettingsSchema.shape.statistics}>
          {(field) => (
            <SettingsToggle
              field={field}
              label="Enable Statistics"
              description="Collect anonymous usage statistics to help improve the app"
              disabled={disabled}
            />
          )}
        </BgtFormField>
      </SettingsSection>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
        <BgtButton onClick={() => window.open('https://github.com/mregni/BoardGameTracker/issues')} variant="primary">
          <GitHubIcon className="size-4" />
          Report a Bug
        </BgtButton>
        <BgtButton onClick={() => window.open('https://crowdin.com/project/boardgametracker')} variant="primary">
          <CrowdinIcon className="size-4" />
          Help Translate
        </BgtButton>
      </div>
    </div>
  );
};
