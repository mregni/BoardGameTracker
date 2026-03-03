import { Text } from "@radix-ui/themes";
import { cva, cx, type VariantProps } from "class-variance-authority";

import { StringToHsl } from "@/utils/stringUtils";

const avatarVariants = cva("shadow-gray-800 shadow-md", {
  variants: {
    size: {
      small: "h-5 w-5 rounded-xs",
      medium: "h-7 w-7 rounded-md",
      large: "h-11 w-11 rounded-lg",
      big: "h-20 w-20 md:h-28 md:w-28 rounded-full",
    },
    interactive: {
      true: "hover:scale-95 hover:shadow-black hover:shadow-lg hover:cursor-pointer",
      false: "",
    },
    disabled: {
      true: "opacity-50",
      false: "",
    },
    hasImage: {
      true: "cursor-pointer",
      false: "cursor-pointer flex justify-center items-center",
    },
  },
  defaultVariants: {
    size: "medium",
    interactive: false,
    disabled: false,
    hasImage: true,
  },
});

export interface Props extends Omit<
  VariantProps<typeof avatarVariants>,
  "interactive" | "hasImage"
> {
  title?: string;
  image: string | undefined | null;
  onClick?: () => void;
  withTitle?: boolean;
}

const TEXT_SIZE_MAP: Record<
  string,
  "1" | "2" | "3" | "4" | "5" | "6" | "7" | "8" | "9"
> = {
  big: "8",
  large: "5",
  medium: "3",
  small: "2",
};

export const BgtAvatar = (props: Props) => {
  const { title, image, onClick, size, disabled, withTitle = false } = props;

  if (!image && !title) return null;

  const avatarClasses = avatarVariants({
    size,
    interactive: !!onClick,
    disabled,
    hasImage: !!image,
  });

  const textSize = TEXT_SIZE_MAP[size || "medium"];

  return (
    <div
      className={cx(
        "group flex relative min-w-7 flex-row- gap-2 items-center",
        onClick && "cursor-pointer",
      )}
      onClick={onClick}
    >
      {image && <img className={avatarClasses} src={image} alt={title || ""} />}
      {!image && title && (
        <div
          style={{ backgroundColor: StringToHsl(title) }}
          className={avatarClasses}
        >
          <Text size={textSize} className="capitalize">
            {title[0]}
          </Text>
        </div>
      )}
      {withTitle && title && (
        <span className={cx(onClick && "cursor-pointer")}>{title}</span>
      )}
    </div>
  );
};
