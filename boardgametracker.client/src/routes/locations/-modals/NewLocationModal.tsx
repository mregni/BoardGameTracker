import { useTranslation } from 'react-i18next';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';

import { useLocationModal } from '../-hooks/useLocationModal';

import { useToasts } from '@/routes/-hooks/useToasts';
import { CreateLocation, CreateLocationSchema } from '@/models';
import { BgtInputField } from '@/components/BgtForm/BgtInputField';
import { BgtDialog, BgtDialogContent, BgtDialogTitle, BgtDialogClose } from '@/components/BgtDialog/BgtDialog';
import BgtButton from '@/components/BgtButton/BgtButton';

interface Props {
  open: boolean;
  close: () => void;
}

export const NewLocationModal = (props: Props) => {
  const { open, close } = props;
  const { successToast, errorToast } = useToasts();
  const { t } = useTranslation();

  const onSaveSuccess = () => {
    successToast('location.notifications.created');
    close();
  };
  const onSaveError = () => {
    errorToast('location.notifications.create-failed');
  };

  const { saveLocation, isLoading } = useLocationModal({ onSaveSuccess, onSaveError });

  const { handleSubmit, control } = useForm<CreateLocation>({
    resolver: zodResolver(CreateLocationSchema),
    defaultValues: {
      name: '',
    },
  });

  const onSubmit = async (data: CreateLocation) => {
    await saveLocation(data);
  };

  return (
    <BgtDialog open={open}>
      <BgtDialogContent>
        <form onSubmit={(event) => void handleSubmit(onSubmit)(event)} className="w-full">
          <BgtDialogTitle>{t('location.new.title')}</BgtDialogTitle>
          <div className="flex flex-col gap-2 mb-3">
            <BgtInputField
              type="text"
              control={control}
              name="name"
              label={t('location.new.name.placeholder')}
              disabled={isLoading}
            />
          </div>
          <BgtDialogClose>
            <BgtButton variant="soft" color="cancel" onClick={() => close()} disabled={isLoading}>
              {t('common.cancel')}
            </BgtButton>
            <BgtButton color="primary" type="submit" disabled={isLoading}>
              {t('common.save')}
            </BgtButton>
          </BgtDialogClose>
        </form>
      </BgtDialogContent>
    </BgtDialog>
  );
};
