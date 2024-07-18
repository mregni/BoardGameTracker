import { BgtText } from '../BgtText/BgtText';
import { BgtAvatar } from '../BgtAvatar/BgtAvatar';
import { StringToHsl } from '../../utils/stringUtils';

interface Props {
  image: string | undefined | null;
  name: string | undefined;
  value: string | number | undefined;
  nameHeader: string;
  valueHeader: string;
}

export const BgtMostWinnerCard = (props: Props) => {
  const { image, name, value, nameHeader, valueHeader } = props;

  if (name === undefined) return null;

  return (
    <div className="flex flex-row justify-between items-center bg-gradient-to-r from-[#9A02FB1A] to-[#09FFC41A] rounded-lg p-3">
      <div className="flex flex-row gap-2">
        <BgtAvatar image={image} title={name} color={StringToHsl(name)} size="large" />
        <div className="flex flex-col justify-center gap-0">
          <BgtText weight="medium" size="3" className="uppercase">
            {name}
          </BgtText>
          <BgtText size="1" className="uppercase text-mint-green">
            {nameHeader}
          </BgtText>
        </div>
      </div>
      <div className="flex flex-col gap2 items-end">
        <BgtText size="3" className="uppercase">
          {valueHeader}
        </BgtText>
        <BgtText size="1" className="uppercase text-lime-green">
          {value}
        </BgtText>
      </div>
    </div>
  );
};
