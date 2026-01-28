import { SettingsToggle } from './SettingsToggle';
import { SettingsSection } from './SettingsSection';

import { SettingsSchema } from '@/models';
import { BgtFormField, BgtInputField } from '@/components/BgtForm';

interface Props {
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  form: any;
  disabled?: boolean;
}

export const ShelfOfShameSettings = ({ form, disabled = false }: Props) => {
  return (
    <div className="space-y-6">
      <SettingsSection
        title="Shelf of Shame Tracking"
        description="Configure how unplayed games are tracked and displayed."
      >
        <BgtFormField form={form} name="shelfOfShameEnabled" schema={SettingsSchema.shape.shelfOfShameEnabled}>
          {(field) => (
            <SettingsToggle
              field={field}
              label="Enable Shelf of Shame"
              description="Track games that haven't been played in a while"
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
                schema={SettingsSchema.shape.shelfOfShameMonthsLimit}
              >
                {(field) => (
                  <BgtInputField
                    field={field}
                    disabled={disabled}
                    type="number"
                    label="Months Threshold"
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
