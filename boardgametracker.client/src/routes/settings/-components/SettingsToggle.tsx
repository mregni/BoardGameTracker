import { useCallback } from 'react';
import { AnyFieldApi } from '@tanstack/react-form';
import * as Switch from '@radix-ui/react-switch';

import { BgtText } from '@/components/BgtText/BgtText';

interface Props {
  field: AnyFieldApi;
  label: string;
  description: string;
  disabled?: boolean;
}

export const SettingsToggle = ({ field, label, description, disabled = false }: Props) => {
  const handleCheckedChange = useCallback(
    (checked: boolean) => {
      field.handleChange(checked);
    },
    [field]
  );

  return (
    <div className="flex items-center justify-between p-3 bg-background rounded-lg border border-white/10 gap-3">
      <Switch.Root
        onCheckedChange={handleCheckedChange}
        disabled={disabled}
        checked={field.state.value}
        className="relative inline-flex h-7 w-12 shrink-0 items-center rounded-full transition-colors focus:outline-none focus:ring-2 focus:ring-primary focus:ring-offset-2 focus:ring-offset-background data-[state=checked]:bg-primary bg-white/10"
      >
        <Switch.Thumb className="inline-block h-5 w-5 transform rounded-full bg-white transition-transform data-[state=checked]:translate-x-6 translate-x-1" />
      </Switch.Root>
      <div className="flex-1 pr-3">
        <BgtText size="2" weight="medium" color="white">
          {label}
        </BgtText>
        <BgtText size="1" color="white" opacity={50}>
          {description}
        </BgtText>
      </div>
    </div>
  );
};
