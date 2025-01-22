import { useTranslation } from 'react-i18next';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';

import { useToast } from '@/providers/BgtToastProvider';
import { CreateLocation, CreateLocationSchema, Location } from '@/models';
import { useLocations } from '@/hooks/useLocations';
import { BgtInputField } from '@/components/BgtForm/BgtInputField';
import { BgtDialog, BgtDialogClose, BgtDialogContent, BgtDialogTitle } from '@/components/BgtDialog/BgtDialog';
import BgtButton from '@/components/BgtButton/BgtButton';

interface Props {
  location: Location;
  open: boolean;
  close: () => void;
}

export const EditLocationModal = (props: Props) => {
  const { location, open, close } = props;
  const { t } = useTranslation();
  const { showInfoToast, showErrorToast } = useToast();

  const onEditSuccess = () => {
    showInfoToast('location.notifications.update');
    close();
  };
  const onEditFailed = () => {
    showErrorToast('location.notifications.update-failed');
  };
  const { update } = useLocations({ onEditSuccess, onEditFailed });

  const { handleSubmit, control } = useForm<CreateLocation>({
    resolver: zodResolver(CreateLocationSchema),
    defaultValues: {
      name: location.name,
    },
  });

  if (location === null) return null;

  const onSubmit = async (data: CreateLocation) => {
    location.name = data.name;
    await update(location);
  };

  return (
    <BgtDialog open={open}>
      <BgtDialogContent>
        <form onSubmit={(event) => void handleSubmit(onSubmit)(event)} className="w-full">
          <BgtDialogTitle>{t('location.edit.title')}</BgtDialogTitle>
          <div className="flex flex-col gap-2 mb-3">
            <BgtInputField type="text" control={control} name="name" label={t('location.new.name.placeholder')} />
          </div>
          <BgtDialogClose>
            <BgtButton variant="soft" color="cancel" onClick={() => close()}>
              {t('common.cancel')}
            </BgtButton>
            <BgtButton color="primary" type="submit">
              {t('common.save')}
            </BgtButton>
          </BgtDialogClose>
        </form>
      </BgtDialogContent>
    </BgtDialog>
  );
};
