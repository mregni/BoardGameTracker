import { useTranslation } from 'react-i18next';
import { useNavigate } from '@tanstack/react-router';

import {
  BgtDialog,
  BgtDialogContent,
  BgtDialogDescription,
  BgtDialogClose,
  BgtDialogTitle,
} from '@/components/BgtDialog/BgtDialog';
import BgtButton from '@/components/BgtButton/BgtButton';
import { BgtSelect } from '@/components/BgtForm/BgtSelect';
import { useForm } from 'react-hook-form';
import { CreateLoan, CreateLoanSchema } from '@/models/Loan/CreateLoan';
import { zodResolver } from '@hookform/resolvers/zod';
import { useNewLoanModal } from '../-hooks.tsx/useNewLoanModal';
import { useState } from 'react';
import { BgtInputField } from '@/components/BgtForm/BgtInputField';
import { Bars } from 'react-loading-icons';
import { Loan } from '@/models/Loan/Loan';
import { useToasts } from '@/routes/-hooks/useToasts';

interface Props {
  open: boolean;
  setOpen: React.Dispatch<React.SetStateAction<boolean>>;
}

const NewLoanModal = (props: Props) => {
  const { open, setOpen } = props;
  const { t } = useTranslation();
  const [formDisabled, setFormDisabled] = useState(false);
  const { errorToast, successToast } = useToasts();

  const onSaveError = () => {
    errorToast('loan.notifications.create-failed');
  };

  const onSaveSuccess = (data: Loan) => {
    successToast('loan.notifications.created');
  };

  const { games, players, isLoading, saveLoan } = useNewLoanModal({ onSaveError, onSaveSuccess });

  const { handleSubmit, control } = useForm<CreateLoan>({
    resolver: zodResolver(CreateLoanSchema),
  });

  const onSubmit = async (data: CreateLoan) => {
    setFormDisabled(true);
    console.log(data);
    await saveLoan(data);
    setFormDisabled(false);
  };

  return (
    <BgtDialog open={open}>
      <BgtDialogContent>
        <form onSubmit={(event) => void handleSubmit(onSubmit)(event)} className="w-full">
          <BgtDialogTitle>{t('loan.new.title')}</BgtDialogTitle>
          <BgtDialogDescription>{t('loan.new.description')}</BgtDialogDescription>
          <div className="flex flex-col gap-4 mt-3 mb-3">
            <div className="flex flex-col gap-3 w-full">
              <BgtSelect
                hasSearch
                control={control}
                name="gameId"
                items={games.map((x) => ({
                  value: x.id.toString(),
                  label: x.title,
                  image: x.image,
                }))}
                label={t('player-session.new.game.label')}
                disabled={formDisabled}
                placeholder={t('player-session.new.game.placeholder')}
              />
              <BgtSelect
                hasSearch
                control={control}
                name="playerId"
                items={players.map((x) => ({
                  value: x.id.toString(),
                  label: x.name,
                  image: x.image,
                }))}
                label={t('loan.new.player.label')}
                disabled={formDisabled}
                placeholder={t('loan.new.player.placeholder')}
              />
              <BgtInputField
                label={t('loan.new.start.label')}
                name="loanDate"
                type="date"
                control={control}
                disabled={formDisabled}
                className="pr-2"
              />
              <BgtInputField
                label={t('loan.new.end.label')}
                name="returnDate"
                type="date"
                control={control}
                disabled={formDisabled}
                className="pr-2"
              />
            </div>
          </div>
          <BgtDialogClose>
            <BgtButton
              disabled={isLoading || formDisabled}
              variant="soft"
              color="cancel"
              className="flex-1"
              onClick={() => setOpen(false)}
            >
              {t('common.cancel')}
            </BgtButton>
            <BgtButton type="submit" disabled={isLoading || formDisabled} className="flex-1" variant="soft">
              {formDisabled && <Bars className="size-4" />}
              Create loan
            </BgtButton>
          </BgtDialogClose>
        </form>
      </BgtDialogContent>
    </BgtDialog>
  );
};

export default NewLoanModal;
