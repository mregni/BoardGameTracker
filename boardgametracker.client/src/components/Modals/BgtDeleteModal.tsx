import { useTranslation } from 'react-i18next';

import {
  BgtDialog,
  BgtDialogClose,
  BgtDialogContent,
  BgtDialogDescription,
  BgtDialogTitle,
} from '../BgtDialog/BgtDialog';
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
    <BgtDialog open={open}>
      <BgtDialogContent>
        <BgtDialogTitle>{t('common.delete.title', { title: title })}</BgtDialogTitle>
        <BgtDialogDescription>{t('common.delete.description', { title: title })}</BgtDialogDescription>
        <BgtDialogClose>
          <BgtButton color="error" onClick={onDelete}>
            {t('common.delete.button')}
          </BgtButton>
          <BgtButton color="primary" variant="inline" onClick={() => setOpen(false)}>
            {t('common.cancel')}
          </BgtButton>
        </BgtDialogClose>
      </BgtDialogContent>
    </BgtDialog>
  );
};
