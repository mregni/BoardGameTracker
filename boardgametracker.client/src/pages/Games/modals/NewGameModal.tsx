import { useTranslation } from 'react-i18next';

import {
  BgtDialog,
  BgtDialogContent,
  BgtDialogDescription,
  BgtDialogClose,
  BgtDialogTitle,
} from '@/components/BgtDialog/BgtDialog';
import BgtButton from '@/components/BgtButton/BgtButton';
import BgtBigButton from '@/components/BgtButton/BgtBigButton';

interface Props {
  open: boolean;
  setOpen: React.Dispatch<React.SetStateAction<boolean>>;
  openBgg: () => void;
  openManual: () => void;
}

const NewGameModal = (props: Props) => {
  const { open, setOpen, openBgg, openManual } = props;
  const { t } = useTranslation();

  return (
    <BgtDialog open={open}>
      <BgtDialogContent>
        <BgtDialogTitle>{t('game.new.title')}</BgtDialogTitle>
        <BgtDialogDescription>{t('game.new.description')}</BgtDialogDescription>
        <div className="flex flex-col gap-4 mt-3 mb-3">
          <BgtBigButton title={t('game.new.bgg-title')} subText={t('game.new.bgg-subtext')} onClick={openBgg} />
          <BgtBigButton
            title={t('game.new.manual-title')}
            subText={t('game.new.manual-subtext')}
            onClick={openManual}
          />
        </div>
        <BgtDialogClose>
          <BgtButton variant="soft" color="cancel" className="w-full" onClick={() => setOpen(false)}>
            {t('common.cancel')}
          </BgtButton>
        </BgtDialogClose>
      </BgtDialogContent>
    </BgtDialog>
  );
};

export default NewGameModal;
