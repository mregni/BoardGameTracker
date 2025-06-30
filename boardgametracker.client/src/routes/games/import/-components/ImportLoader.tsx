import { Bars } from 'react-loading-icons';
import { ComponentPropsWithoutRef } from 'react';

interface Props extends ComponentPropsWithoutRef<'div'> {
  show: () => boolean;
  text: string;
}

export const ImportLoader = ({ children, text, show }: Props) => {
  if (show()) {
    return (
      <div className="flex flex-col justify-center items-center w-full h-full">
        <Bars />
        <span>{text}</span>
      </div>
    );
  }

  return <>{children}</>;
};
