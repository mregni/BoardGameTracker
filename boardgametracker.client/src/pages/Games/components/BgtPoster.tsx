import { ComponentPropsWithoutRef } from 'react';
import { cx } from 'class-variance-authority';

import { StringToRgb } from '../../../utils/stringUtils';

interface Props extends ComponentPropsWithoutRef<'div'> {
  title: string;
  image: string | null;
}

export const BgtPoster = (props: Props) => {
  const { className, title, image } = props;

  return (
    <div
      style={{ '--image-url': `url(${image})`, '--fallback-color': StringToRgb(title) }}
      className={cx(
        className,
        'relative overflow-hidden aspect-square rounded-xl flex justify-center flex-col px-3 w-full bg-cover bg-no-repeat bg-center',
        image && 'bg-[image:var(--image-url)]',
        !image && `bg-[var(--fallback-color)]`
      )}
    >
      {!image && (
        <span className="flex justify-center align-middle h-max font-bold text-3xl capitalize">{title[0]}</span>
      )}
    </div>
  );
};
