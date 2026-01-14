import { ComponentPropsWithoutRef, ReactNode } from 'react';
import { cx } from 'class-variance-authority';
import { Dialog } from '@radix-ui/themes';

interface BgtDialogProps {
  open: boolean;
  children: ReactNode;
}

export const BgtDialog = (props: BgtDialogProps) => {
  const { open, children } = props;

  return <Dialog.Root open={open}>{children}</Dialog.Root>;
};

type BgtDialogContentProps = ComponentPropsWithoutRef<typeof Dialog.Content>;

export const BgtDialogContent = (props: BgtDialogContentProps) => {
  const { className, children, ...rest } = props;
  return (
    <Dialog.Content className={cx('bg-dialog!', className)} {...rest}>
      {children}
    </Dialog.Content>
  );
};

type BgtDialogTitleProps = ComponentPropsWithoutRef<typeof Dialog.Title>;

export const BgtDialogTitle = (props: BgtDialogTitleProps) => {
  const { className, children, ...rest } = props;
  return (
    <Dialog.Title className={cx('text-2xl font-bold uppercase', className)} {...rest}>
      {children}
    </Dialog.Title>
  );
};

type BgtDialogDescriptionProps = ComponentPropsWithoutRef<typeof Dialog.Description>;

export const BgtDialogDescription = (props: BgtDialogDescriptionProps) => {
  const { className, children, ...rest } = props;
  return (
    <Dialog.Description className={cx(className)} {...rest}>
      {children}
    </Dialog.Description>
  );
};

interface BgtDialogCloseProps {
  className?: string;
  children: ReactNode;
}

export const BgtDialogClose = (props: BgtDialogCloseProps) => {
  const { className, children } = props;

  return <div className={cx('flex justify-between pt-2 gap-3', className)}>{children}</div>;
};
