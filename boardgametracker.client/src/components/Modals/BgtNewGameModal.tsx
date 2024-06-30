import { useTranslation } from 'react-i18next';

import { Button, Dialog } from '@radix-ui/themes';

import BgtBigButton from '../BgtButton/BgtBigButton';

interface Props {
  open: boolean;
  setOpen: React.Dispatch<React.SetStateAction<boolean>>;
  openBgg: () => void;
  openManual: () => void;
}

const BgtNewGameModal = (props: Props) => {
  const { open, setOpen, openBgg, openManual } = props;
  const { t } = useTranslation();

  return (
    <Dialog.Root open={open}>
      <Dialog.Content>
        <Dialog.Title>{t('game.new.title')}</Dialog.Title>
        <Dialog.Description>{t('game.new.description')}</Dialog.Description>
        <div className="flex flex-col gap-4 mt-3 mb-3">
          <BgtBigButton title={t('game.new.bgg-title')} subText={t('game.new.bgg-subtext')} onClick={openBgg} />
          <BgtBigButton disabled title={t('game.new.manual-title')} subText={t('game.new.manual-subtext')} onClick={openManual} />
        </div>
        <Dialog.Close>
          <Button variant="surface" color="gray" className="w-full hover:cursor-pointer" onClick={() => setOpen(false)}>
            {t('common.cancel')}
          </Button>
        </Dialog.Close>
      </Dialog.Content>
    </Dialog.Root>
  );
};

export default BgtNewGameModal;
