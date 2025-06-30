import { HTMLAttributes } from 'react';
import { cx } from 'class-variance-authority';
import { Dialog } from '@radix-ui/themes';

interface BgtDialogProps extends HTMLAttributes<HTMLDivElement> {
  open: boolean;
}

const BgtDialog = (props: BgtDialogProps) => {
  const { open, children, ...rest } = props;

  return (
    <Dialog.Root open={open} {...rest}>
      {children}
    </Dialog.Root>
  );
};

const BgtDialogContent = (props: HTMLAttributes<HTMLDivElement>) => {
  const { className, children, ...rest } = props;
  return (
    <Dialog.Content className={cx(className, 'bg-card-black')} {...rest}>
      {children}
    </Dialog.Content>
  );
};

const BgtDialogTitle = (props: HTMLAttributes<HTMLDivElement>) => {
  const { className, children } = props;
  return <Dialog.Title className={cx('text-2xl font-bold uppercase', className)}>{children}</Dialog.Title>;
};

const BgtDialogDescription = (props: HTMLAttributes<HTMLDivElement>) => {
  const { className, children } = props;
  return <Dialog.Description className={cx(className)}>{children}</Dialog.Description>;
};

const BgtDialogClose = (props: HTMLAttributes<HTMLDivElement>) => {
  const { className, children, ...rest } = props;

  return (
    <div className={cx('flex gap-3 pt-2', className)} {...rest}>
      <Dialog.Close>
        <>{children}</>
      </Dialog.Close>
    </div>
  );
};

export { BgtDialog, BgtDialogContent, BgtDialogDescription, BgtDialogClose, BgtDialogTitle };
