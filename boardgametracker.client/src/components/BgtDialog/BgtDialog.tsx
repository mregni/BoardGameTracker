import { ComponentPropsWithoutRef, createContext, ReactNode, useContext } from 'react';
import { cx } from 'class-variance-authority';
import { Dialog } from '@radix-ui/themes';

import Cross from '@/assets/icons/x.svg?react';

const DialogCloseContext = createContext<(() => void) | undefined>(undefined);

interface BgtDialogProps {
  open: boolean;
  children: ReactNode;
  onClose?: () => void;
}

export const BgtDialog = (props: BgtDialogProps) => {
  const { open, children, onClose } = props;

  return (
    <Dialog.Root open={open}>
      <DialogCloseContext.Provider value={onClose}>{children}</DialogCloseContext.Provider>
    </Dialog.Root>
  );
};

export const BgtDialogContent = (props: ComponentPropsWithoutRef<typeof Dialog.Content>) => {
  const { className, children, ...rest } = props;
  const onClose = useContext(DialogCloseContext);
  return (
    <Dialog.Content className={cx('bg-dialog! relative', className)} {...rest}>
      {onClose && (
        <button
          onClick={onClose}
          className="absolute top-1 right-1 p-1 hover:bg-white/10 rounded-lg transition-colors cursor-pointer"
        >
          <Cross className="size-5" />
        </button>
      )}
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
