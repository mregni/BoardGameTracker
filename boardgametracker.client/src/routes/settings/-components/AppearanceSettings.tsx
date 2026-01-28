import { SettingsSection } from './SettingsSection';

import { SettingsSchema } from '@/models';
import { BgtFormField } from '@/components/BgtForm';
import { BgtText } from '@/components/BgtText/BgtText';
import BgtButton from '@/components/BgtButton/BgtButton';

interface Props {
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  form: any;
  disabled?: boolean;
}

export const AppearanceSettings = ({ form, disabled = false }: Props) => {
  return (
    <div className="space-y-6">
      <SettingsSection title="Theme Settings" description="Customize the appearance of your application.">
        <BgtFormField form={form} name="primaryColor" schema={SettingsSchema.shape.primaryColor}>
          {(field) => (
            <div>
              <BgtText size="2" weight="medium" color="white" className="mb-3">
                Primary Color
              </BgtText>
              <div className="flex items-center gap-3">
                <input
                  type="color"
                  value={field.state.value}
                  onChange={(e) => field.handleChange(e.target.value)}
                  disabled={disabled}
                  className="h-12 w-20 rounded-lg border border-white/10 bg-background cursor-pointer"
                />
                <input
                  type="text"
                  value={field.state.value}
                  onChange={(e) => field.handleChange(e.target.value)}
                  disabled={disabled}
                  placeholder="#a855f7"
                  className="flex-1 px-3 py-2 bg-background border border-white/10 rounded-lg text-white placeholder-gray-500 focus:outline-none focus:border-primary transition-colors font-mono text-sm"
                />
              </div>
              <BgtText size="1" color="white" opacity={50} className="mt-2">
                Enter a hex color code or use the color picker
              </BgtText>

              <div className="mt-6 p-4 bg-background border border-white/10 rounded-lg">
                <BgtText size="1" color="white" opacity={50} className="mb-3">
                  Preview
                </BgtText>
                <div className="flex flex-wrap gap-2">
                  <BgtButton style={{ backgroundColor: field.state.value }}>Primary Button</BgtButton>
                  <div
                    className="px-4 py-2 rounded-lg border"
                    style={{ borderColor: field.state.value, color: field.state.value }}
                  >
                    Outlined Element
                  </div>
                  <div
                    className="w-12 h-12 rounded-full"
                    style={{ backgroundColor: `${field.state.value}20`, border: `2px solid ${field.state.value}` }}
                  />
                </div>
              </div>
            </div>
          )}
        </BgtFormField>
      </SettingsSection>
    </div>
  );
};
