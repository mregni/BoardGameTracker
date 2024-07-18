import { useTranslation } from 'react-i18next';
import { Dialog } from '@radix-ui/themes';

import { BgtHeading } from '../BgtHeading/BgtHeading';
import BgtButton from '../BgtButton/BgtButton';

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
      <Dialog.Content className="bg-card-black">
        <BgtHeading size="6" className="uppercase">
          {t('common.delete.title', { title: title })}
        </BgtHeading>
        <Dialog.Description>{t('common.delete.description', { title: title })}</Dialog.Description>
        <div className="flex justify-end gap-3 mt-3">
          <Dialog.Close>
            <>
              <BgtButton color="error" onClick={onDelete}>
                {t('common.delete.button')}
              </BgtButton>
              <BgtButton color="primary" variant="inline" onClick={() => setOpen(false)}>
                {t('common.cancel')}
              </BgtButton>
            </>
          </Dialog.Close>
        </div>
      </Dialog.Content>
    </Dialog.Root>
  );
};
