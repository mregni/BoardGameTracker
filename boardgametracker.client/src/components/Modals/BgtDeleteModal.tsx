import { useTranslation } from 'react-i18next';

import { Button, Dialog } from '@radix-ui/themes';

interface Props {
  open: boolean;
  setOpen: React.Dispatch<React.SetStateAction<boolean>>;
  onDelete: () => void;
  title: string;
}

export const BgtDeleteModal = (props: Props) => {
  const { open, setOpen, onDelete, title } = props;
  const { t } = useTranslation();

  return (
    <Dialog.Root open={open}>
      <Dialog.Content>
        <Dialog.Title>{t('common.delete.title', { title: title })}</Dialog.Title>
        <Dialog.Description>{t('common.delete.description', { title: title })}</Dialog.Description>
        <div className="flex justify-end gap-3 mt-3">
          <Dialog.Close>
            <>
              <Button type="submit" variant="solid" color="red" onClick={onDelete}>
                {t('common.delete.button')}
              </Button>
              <Button variant="surface" color="gray" onClick={() => setOpen(false)}>
                {t('common.cancel')}
              </Button>
            </>
          </Dialog.Close>
        </div>
      </Dialog.Content>
    </Dialog.Root>
  );
};
