import { ComponentType, SVGProps } from 'react';

import BgtButton from '../BgtButton/BgtButton';

interface Props {
  icon: ComponentType<SVGProps<SVGSVGElement>>;
  title: string;
  description: string;
  action?: {
    label: string;
    onClick: () => void;
  };
}

export const BgtEmptyState = ({ icon: Icon, title, description, action }: Props) => {
  return (
    <div className="flex items-center justify-center min-h-[400px] p-8">
      <div className="text-center max-w-md">
        <div className="inline-flex items-center justify-center w-20 h-20 bg-primary/10 border-2 border-primary/20 rounded-xl mb-6">
          <Icon className="text-primary size-16" />
        </div>
        <h3 className="text-2xl font-bold text-white mb-3">{title}</h3>
        <p className="text-white/60 mb-6">{description}</p>
        {action && (
          <BgtButton size="3" onClick={action.onClick}>
            {action.label}
          </BgtButton>
        )}
      </div>
    </div>
  );
};
