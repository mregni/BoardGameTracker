import { ComponentPropsWithoutRef } from 'react';
import clsx from 'clsx';

import { StringToRgb } from '../../../utils/stringUtils';

interface Props extends ComponentPropsWithoutRef<'div'> {
  title: string;
  image: string;
}

export const BgtPoster = (props: Props) => {
  const { className, title, image } = props;

  return (
    <div
      style={{ '--image-url': `url(${image})`, '--fallback-color': StringToRgb(title) }}
      className={clsx(
        className,
        'relative overflow-hidden aspect-square rounded-xl flex justify-end flex-col px-3 w-full bg-cover bg-no-repeat bg-center',
        image && 'bg-[image:var(--image-url)]',
        !image && `bg-[var(--fallback-color)]`
      )}
    ></div>
  );
};
