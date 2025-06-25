import { toast } from 'sonner';
import { useTranslation } from 'react-i18next';

export const useToasts = () => {
  const { t } = useTranslation();

  const successToast = (message: string) => {
    toast.success(t(message));
  };

  const errorToast = (message: string) => {
    toast.error(t(message));
  };

  const infoToast = (message: string) => {
    toast.info(t(message));
  };

  const warningToast = (message: string) => {
    toast.warning(t(message));
  };

  return {
    successToast,
    errorToast,
    infoToast,
    warningToast,
  };
};
