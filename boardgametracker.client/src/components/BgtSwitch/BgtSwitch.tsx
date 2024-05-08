import { Control, Controller, FieldValues, Path } from 'react-hook-form';

import { Switch, Text } from '@radix-ui/themes';

interface Props<T extends FieldValues> {
  label: string;
  control?: Control<T>;
  name: Path<T>;
  disabled?: boolean;
  defaultValue?: boolean;
  className?: string;
}

export const BgtSwitch = <TFormValues extends FieldValues>(props: Props<TFormValues>) => {
  const { label, control, name, disabled = false, defaultValue = false, className } = props;

  return (
    <div className={className}>
      <Controller
        name={name}
        control={control}
        render={({ field }) => (
          <Text as="label" size="3">
            <div className="flex gap-2">
              <Switch size="2" onCheckedChange={field.onChange} disabled={disabled} defaultChecked={defaultValue} />
              {label}
            </div>
          </Text>
        )}
      />
    </div>
  );
};
