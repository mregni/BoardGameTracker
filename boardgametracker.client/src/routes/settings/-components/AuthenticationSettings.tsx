import { SettingsToggle } from './SettingsToggle';
import { SettingsSection } from './SettingsSection';

import { SettingsSchema } from '@/models';
import { BgtFormField, BgtInputField } from '@/components/BgtForm';

interface Props {
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  form: any;
  disabled?: boolean;
}

export const AuthenticationSettings = ({ form, disabled = false }: Props) => {
  return (
    <div className="space-y-6">
      <SettingsSection title="OpenID Connect (OIDC)" description="Configure single sign-on with an OIDC provider.">
        <BgtFormField form={form} name="oidcEnabled" schema={SettingsSchema.shape.oidcEnabled}>
          {(field) => (
            <SettingsToggle
              field={field}
              label="Enable OIDC Authentication"
              description="Use external identity provider for authentication"
              disabled={disabled}
            />
          )}
        </BgtFormField>

        <form.Subscribe selector={(state: { values: { oidcEnabled: boolean } }) => state.values.oidcEnabled}>
          {(oidcEnabled: boolean) =>
            oidcEnabled && (
              <>
                <BgtFormField form={form} name="oidcProvider" schema={SettingsSchema.shape.oidcProvider}>
                  {(field) => (
                    <BgtInputField
                      field={field}
                      disabled={disabled}
                      type="text"
                      label="Provider URL"
                      placeholder="https://accounts.example.com"
                    />
                  )}
                </BgtFormField>

                <BgtFormField form={form} name="oidcClientId" schema={SettingsSchema.shape.oidcClientId}>
                  {(field) => (
                    <BgtInputField
                      field={field}
                      disabled={disabled}
                      type="text"
                      label="Client ID"
                      placeholder="your-client-id"
                    />
                  )}
                </BgtFormField>

                <BgtFormField form={form} name="oidcClientSecret" schema={SettingsSchema.shape.oidcClientSecret}>
                  {(field) => (
                    <BgtInputField
                      field={field}
                      disabled={disabled}
                      type="text"
                      label="Client Secret"
                      placeholder="••••••••••••••••"
                    />
                  )}
                </BgtFormField>
              </>
            )
          }
        </form.Subscribe>
      </SettingsSection>
    </div>
  );
};
