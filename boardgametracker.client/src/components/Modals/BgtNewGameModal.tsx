import { useTranslation } from 'react-i18next';
import { Button, Dialog } from '@radix-ui/themes';

import { BgtHeading } from '../BgtHeading/BgtHeading';
import BgtButton from '../BgtButton/BgtButton';
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
      <Dialog.Content className="bg-card-black">
        <BgtHeading size="6" className="uppercase">
          {t('game.new.title')}
        </BgtHeading>
        <Dialog.Description>{t('game.new.description')}</Dialog.Description>
        <div className="flex flex-col gap-4 mt-3 mb-3">
          <BgtBigButton title={t('game.new.bgg-title')} subText={t('game.new.bgg-subtext')} onClick={openBgg} />
          <BgtBigButton
            disabled
            title={t('game.new.manual-title')}
            subText={t('game.new.manual-subtext')}
            onClick={openManual}
          />
        </div>
        <Dialog.Close>
          <BgtButton
            variant="soft"
            color="cancel"
            className="w-full hover:cursor-pointer"
            onClick={() => setOpen(false)}
          >
            {t('common.cancel')}
          </BgtButton>
        </Dialog.Close>
      </Dialog.Content>
    </Dialog.Root>
  );
};

export default BgtNewGameModal;
