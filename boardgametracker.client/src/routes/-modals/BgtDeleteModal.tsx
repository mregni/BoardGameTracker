import { useTranslation } from 'react-i18next';

import {
  BgtDialog,
  BgtDialogClose,
  BgtDialogContent,
  BgtDialogDescription,
  BgtDialogTitle,
} from '../../components/BgtDialog/BgtDialog';
import BgtButton from '../../components/BgtButton/BgtButton';

interface Props {
  open: boolean;
  close: () => void;
  onDelete: (() => Promise<void>) | (() => void);
  title: string;
  description: string;
}

export const BgtDeleteModal = (props: Props) => {
  const { open, close, onDelete, title, description } = props;
  const { t } = useTranslation();

  return (
    <BgtDialog open={open}>
      <BgtDialogContent>
        <BgtDialogTitle>{t('common.delete.title', { title: title })}</BgtDialogTitle>
        <BgtDialogDescription>{description}</BgtDialogDescription>
        <BgtDialogClose>
          <BgtButton variant="cancel" onClick={() => close()}>
            {t('common.cancel')}
          </BgtButton>
          <BgtButton variant="error" onClick={onDelete}>
            {t('common.delete.button')}
          </BgtButton>
        </BgtDialogClose>
      </BgtDialogContent>
    </BgtDialog>
  );
};
