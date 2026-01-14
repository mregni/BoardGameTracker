import { useTranslation } from 'react-i18next';
import { useForm } from '@tanstack/react-form';

import { useLocationModal } from '../-hooks/useLocationModal';

import { useToasts } from '@/routes/-hooks/useToasts';
import { CreateLocationSchema, Location } from '@/models';
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
  const { successToast, errorToast } = useToasts();

  const onUpdateSuccess = () => {
    successToast('location.notifications.update');
    close();
  };
  const onUpdateError = () => {
    errorToast('location.notifications.update-failed');
  };
  const { updateLocation, isLoading } = useLocationModal({ onUpdateSuccess, onUpdateError });

  const form = useForm({
    defaultValues: {
      name: location.name,
    },
    onSubmit: async ({ value }) => {
      const validatedData = CreateLocationSchema.parse(value);
      const updatedLocation: Location = {
        ...location,
        name: validatedData.name,
      };
      await updateLocation(updatedLocation);
    },
  });

  if (location === null) return null;

  return (
    <BgtDialog open={open}>
      <BgtDialogContent>
        <form
          onSubmit={(e) => {
            e.preventDefault();
            e.stopPropagation();
            form.handleSubmit();
          }}
          className="w-full"
        >
          <BgtDialogTitle>{t('location.edit.title')}</BgtDialogTitle>
          <div className="flex flex-col gap-2 mb-3">
            <form.Field
              name="name"
              validators={{
                onChange: ({ value }) => {
                  const result = CreateLocationSchema.shape.name.safeParse(value);
                  if (!result.success) {
                    return t(result.error.errors[0].message);
                  }
                  return undefined;
                },
              }}
            >
              {(field) => (
                <BgtInputField
                  field={field}
                  type="text"
                  label={t('location.new.name.placeholder')}
                  disabled={isLoading}
                />
              )}
            </form.Field>
          </div>
          <BgtDialogClose>
            <BgtButton variant="cancel" onClick={() => close()} disabled={isLoading}>
              {t('common.cancel')}
            </BgtButton>
            <BgtButton variant="primary" type="submit" disabled={isLoading}>
              {t('common.save')}
            </BgtButton>
          </BgtDialogClose>
        </form>
      </BgtDialogContent>
    </BgtDialog>
  );
};
