import { Control, Controller, FieldValues, Path, useController } from 'react-hook-form';
import { Select } from '@radix-ui/themes';

import { BgtAvatar } from '../BgtAvatar/BgtAvatar';
import { StringToHsl } from '../../utils/stringUtils';
import { BgtSelectImageItem } from '../../models/Common/BgtSelectItem';

import { BgtFormErrors } from './BgtFormErrors';

interface Props<T extends FieldValues> {
  items: BgtSelectImageItem[];
  name: Path<T>;
  control?: Control<T>;
  disabled: boolean;
}

export const BgtSelectNoLabel = <T extends FieldValues>(props: Props<T>) => {
  const { items, name, control, disabled } = props;

  const {
    fieldState: { error },
  } = useController({ name, control });

  return (
    <div className="grid w-full md:max-w-96">
      <Controller
        name={name}
        control={control}
        render={({ field }) => (
          <Select.Root onValueChange={field.onChange} value={field.value} size="2" defaultValue={control?._defaultValues[name]} disabled={disabled}>
            <Select.Trigger className="line-clamp-1" />
            <Select.Content>
              {items?.map((x) => (
                <Select.Item key={x.value} value={x.value}>
                  <div className="flex flex-row gap-1 items-center">
                    <BgtAvatar image={x.image} color={StringToHsl(x.label)} title={x.label} size="small" />
                    {x.label}
                  </div>
                </Select.Item>
              ))}
            </Select.Content>
          </Select.Root>
        )}
      />
      <div className="flex items-baseline">
        <BgtFormErrors error={error} />
      </div>
    </div>
  );
};
