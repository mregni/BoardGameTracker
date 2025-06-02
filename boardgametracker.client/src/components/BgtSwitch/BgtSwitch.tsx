import { Control, Controller, FieldValues, Path, PathValue } from 'react-hook-form';
import { Text } from '@radix-ui/themes';
import * as Switch from '@radix-ui/react-switch';

interface Props<T extends FieldValues> {
  label: string;
  control?: Control<T>;
  name: Path<T>;
  disabled?: boolean;
  className?: string;
  defaultValue?: PathValue<T, Path<T>>;
}

export const BgtSwitch = <TFormValues extends FieldValues>(props: Props<TFormValues>) => {
  const { label, control, name, disabled = false, className, defaultValue } = props;

  return (
    <div className={className}>
      <Controller
        name={name}
        control={control}
        defaultValue={defaultValue}
        render={({ field }) => (
          <Text as="label" size="3">
            <div className="flex gap-2">
              <Switch.Root
                onCheckedChange={field.onChange}
                disabled={disabled}
                defaultChecked={field.value}
                className="w-[42px] h-[21px] rounded-full relative data-[disabled]:bg-slate-600 data-[state=checked]:bg-primary outline-none cursor-defaul bg-[--gray-10]"
              >
                <Switch.Thumb className="block w-[21px] h-[21px] -left-[2px] top-0 absolute bg-white rounded-full transition-transform duration-100 translate-x-0.5 will-change-transform data-[state=checked]:translate-x-[23px]" />
              </Switch.Root>
              {label}
            </div>
          </Text>
        )}
      />
    </div>
  );
};
