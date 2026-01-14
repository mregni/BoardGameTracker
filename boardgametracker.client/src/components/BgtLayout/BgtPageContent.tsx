import { ReactNode } from 'react';
import { cx } from 'class-variance-authority';

import { BgtDataGuard } from '../BgtDataGuard/BgtDataGuard';

type NonUndefined<T> = T extends undefined ? never : T;
type RequiredData<T> = { [K in keyof T]: NonUndefined<T[K]> };

interface BaseProps {
  className?: string;
  centered?: boolean;
}

interface PropsWithoutGuard extends BaseProps {
  children: ReactNode;
}

interface PropsWithGuard<T extends Record<string, unknown>> extends BaseProps {
  isLoading: boolean;
  data: T;
  children: (data: RequiredData<T>) => ReactNode;
}

type Props<T extends Record<string, unknown> = Record<string, never>> = PropsWithoutGuard | PropsWithGuard<T>;

function isPropsWithGuard<T extends Record<string, unknown>>(props: Props<T>): props is PropsWithGuard<T> {
  return 'data' in props;
}

export const BgtPageContent = <T extends Record<string, unknown> = Record<string, never>>(props: Props<T>) => {
  const { className, centered = false } = props;

  if (isPropsWithGuard(props)) {
    return (
      <div className={cx('flex flex-col gap-3 xl:gap-6', centered && 'h-full', className)}>
        <BgtDataGuard isLoading={props.isLoading} data={props.data}>
          {(validatedData) => <>{props.children(validatedData)}</>}
        </BgtDataGuard>
      </div>
    );
  }

  return <div className={cx('flex flex-col gap-3 xl:gap-6', centered && 'h-full', className)}>{props.children}</div>;
};
