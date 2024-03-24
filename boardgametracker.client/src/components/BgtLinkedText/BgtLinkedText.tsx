import {Link} from 'react-router-dom';

interface Props {
  value: string;
  link: string | number;
}

export const BgtLinkedText = (props: Props) => {
  const { value, link } = props;

  return (
    <Link
    className='text-orange-500 hover:text-orange-700 after:content-[",_"] last:after:content-none'
    to={`/${link}`}>
    {value}
  </Link>
  )
}