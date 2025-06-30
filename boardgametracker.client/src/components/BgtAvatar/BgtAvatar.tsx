import { cx } from 'class-variance-authority';
import { Text } from '@radix-ui/themes';

export interface Props {
  title?: string | undefined;
  image: string | undefined | null;
  color?: string | undefined;
  onClick?: () => void;
  size?: 'small' | 'medium' | 'large' | 'big';
  disabled?: boolean;
}

export const BgtAvatar = (props: Props) => {
  const { title, image, color, onClick, size = 'medium', disabled = false } = props;

  if (!image && title === undefined) return null;

  const getSize = (): '1' | '2' | '3' | '4' | '5' | '6' | '7' | '8' | '9' => {
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

  return (
    <div className="group flex relative min-w-7">
      {image && (
        <img
          className={cx(
            'shadow-gray-800 shadow-md',
            size === 'big' && 'h-28 w-28 rounded-full',
            size === 'large' && 'h-11 w-11 rounded-lg',
            size === 'medium' && 'h-7 w-7 rounded-md',
            size === 'small' && 'h-5 w-5 rounded-sm',
            onClick && 'hover:scale-95 hover:shadow-black hover:shadow-lg hover:cursor-pointer',
            disabled && 'opacity-50'
          )}
          onClick={onClick}
          src={image}
        />
      )}
      {!image && (
        <div
          style={{ backgroundColor: color }}
          onClick={onClick}
          className={cx(
            'shadow-gray-800 shadow-md flex justify-center items-center',
            size === 'big' && 'h-28 w-28 rounded-full',
            size === 'large' && 'h-11 w-11 rounded-sm',
            size === 'medium' && 'h-7 w-7 rounded-sm',
            size === 'small' && 'h-5 w-5 rounded-sm',
            onClick && 'hover:scale-95 hover:shadow-black hover:shadow-lg hover:cursor-pointer'
          )}
        >
          <Text size={getSize()} className="capitalize">
            {title![0]}
          </Text>
        </div>
      )}
    </div>
  );
};
