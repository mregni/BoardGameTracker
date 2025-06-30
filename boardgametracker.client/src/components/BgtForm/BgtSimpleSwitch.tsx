import { cx } from 'class-variance-authority';
import { Text } from '@radix-ui/themes';
import * as Switch from '@radix-ui/react-switch';

interface Props {
  label: string;
  disabled?: boolean;
  className?: string;
  value: boolean;
  onChange: (value: boolean) => void;
}

export const BgtSimpleSwitch = (props: Props) => {
  const { label, disabled = false, className, value, onChange } = props;

  return (
    <div className={cx(className, disabled && 'text-gray-500')}>
      <Text as="label" size="3">
        <div className="flex gap-2">
          <Switch.Root
            onCheckedChange={onChange}
            disabled={disabled}
            defaultChecked={value}
            className="w-[42px] h-[21px] rounded-full relative data-[disabled]:bg-slate-600 data-[state=checked]:bg-primary outline-none cursor-defaul bg-[--gray-10]"
          >
            <Switch.Thumb className="block w-[21px] h-[21px] -left-[2px] top-0 absolute bg-white rounded-full transition-transform duration-100 translate-x-0.5 will-change-transform data-[state=checked]:translate-x-[23px]" />
          </Switch.Root>
          {label}
        </div>
      </Text>
    </div>
  );
};
