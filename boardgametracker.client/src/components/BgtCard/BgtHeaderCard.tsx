import { ReactElement } from 'react';
import { cx } from 'class-variance-authority';

interface Props {
  children: ReactElement | ReactElement[];
  image: string;
}

export const BgtHeaderCard = (props: Props) => {
  const { children, image } = props;

  return (
    <div className="flex flex-col relative">
      <div
        style={{ '--image-url': `url(${image})` }}
        className={cx(
          'rounded-lg border border-solid border-gray-600 p-5 flex flex-col gap-10 z-10',
          'before:rounded-lg before:absolute before:inset-0 before:block before:bg-gradient-to-t before:from-black before:from-15% before:z-[-5] before:opacity-95',
          `bg-cover bg-no-repeat bg-center`,
          image && 'bg-[image:var(--image-url)]'
        )}
      >
        {children}
      </div>
    </div>
  );
};
