import { Bars } from 'react-loading-icons';

export const BgtLoadingSpinner = () => {
  return (
    <div className="flex items-center justify-center min-h-[400px]">
      <Bars className="size-12 text-primary" />
    </div>
  );
};
