import { cva, type VariantProps } from 'class-variance-authority';
import { Text } from '@radix-ui/themes';

const avatarVariants = cva('shadow-gray-800 shadow-md', {
  variants: {
    size: {
      small: 'h-5 w-5 rounded-xs',
      medium: 'h-7 w-7 rounded-md',
      large: 'h-11 w-11 rounded-lg',
      big: 'h-20 w-20 md:h-28 md:w-28 rounded-full',
    },
    interactive: {
      true: 'hover:scale-95 hover:shadow-black hover:shadow-lg hover:cursor-pointer',
      false: '',
    },
    disabled: {
      true: 'opacity-50',
      false: '',
    },
    hasImage: {
      true: '',
      false: 'flex justify-center items-center',
    },
  },
  defaultVariants: {
    size: 'medium',
    interactive: false,
    disabled: false,
    hasImage: true,
  },
});

export interface Props extends Omit<VariantProps<typeof avatarVariants>, 'interactive' | 'hasImage'> {
  title?: string;
  image: string | undefined | null;
  color?: string;
  onClick?: () => void;
}

const TEXT_SIZE_MAP: Record<string, '1' | '2' | '3' | '4' | '5' | '6' | '7' | '8' | '9'> = {
  big: '8',
  large: '5',
  medium: '3',
  small: '2',
};

export const BgtAvatar = (props: Props) => {
  const { title, image, color, onClick, size, disabled } = props;

  if (!image && !title) return null;

  const avatarClasses = avatarVariants({
    size,
    interactive: !!onClick,
    disabled,
    hasImage: !!image,
  });

  const textSize = TEXT_SIZE_MAP[size || 'medium'];

  return (
    <div className="group flex relative min-w-7">
      {image && <img className={avatarClasses} onClick={onClick} src={image} alt={title || ''} />}
      {!image && title && (
        <div style={{ backgroundColor: color }} onClick={onClick} className={avatarClasses}>
          <Text size={textSize} className="capitalize">
            {title[0]}
          </Text>
        </div>
      )}
    </div>
  );
};
