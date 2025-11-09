import { cva, type VariantProps } from 'class-variance-authority';
import { Text } from '@radix-ui/themes';

const avatarVariants = cva(
  'shadow-gray-800 shadow-md',
  {
    variants: {
      size: {
        small: 'h-5 w-5 rounded-sm',
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
  }
);

export interface Props extends Omit<VariantProps<typeof avatarVariants>, 'interactive' | 'hasImage'> {
  title?: string | undefined;
  image: string | undefined | null;
  color?: string | undefined;
  onClick?: () => void;
}

export const BgtAvatar = (props: Props) => {
  const { title, image, color, onClick, size = 'medium', disabled = false } = props;

  if (!image && title === undefined) return null;

  const getTextSize = (): '1' | '2' | '3' | '4' | '5' | '6' | '7' | '8' | '9' => {
    switch (size) {
      case 'big':
        return '8';
      case 'large':
        return '5';
      case 'medium':
        return '3';
      default:
        return '2';
    }
  };

  const avatarClasses = avatarVariants({
    size,
    interactive: !!onClick,
    disabled,
    hasImage: !!image,
  });

  return (
    <div className="group flex relative min-w-7">
      {image && (
        <img
          className={avatarClasses}
          onClick={onClick}
          src={image}
          alt={title || ''}
        />
      )}
      {!image && (
        <div
          style={{ backgroundColor: color }}
          onClick={onClick}
          className={avatarClasses}
        >
          <Text size={getTextSize()} className="capitalize">
            {title![0]}
          </Text>
        </div>
      )}
    </div>
  );
};
