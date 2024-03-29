import clsx from 'clsx';

interface Props {
  src: string;
  onClick?: () => void;
}

interface boe {
  blub: string;
}

export const BgtAvatar = (props: Props) => {
  const { src, onClick } = props;

  const obj: boe = JSON.parse('{ "myString": "string", "myNumber": 4 }') as boe;

  console.log(obj)
  return (
    <>
      <img
        className={
          clsx(
            'w-8 rounded-md shadow-gray-800 shadow-md',
            onClick && 'hover:scale-95 hover:shadow-black hover:shadow-lg hover:cursor-pointer'
          )
        }
        onClick={onClick}
        src={src}
      />
    </>
  )
}