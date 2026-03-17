import { cx } from "class-variance-authority";
import { useEffect, useMemo, useRef, useState } from "react";
import { createPortal } from "react-dom";
import CaretDownIcon from "@/assets/icons/caret-down.svg?react";
import CaretUpIcon from "@/assets/icons/caret-up.svg?react";
import SearchIcon from "@/assets/icons/magnifying-glass.svg?react";
import Cross from "@/assets/icons/x.svg?react";
import { BgtAvatar } from "@/components/BgtAvatar/BgtAvatar";
import { FormFieldWrapper } from "@/components/BgtForm";

interface Option {
	value: number;
	label: string;
	image?: string | null;
}

interface Props {
	label: string;
	options: Option[];
	selected: number[];
	disabled: boolean;
	onChange: (selected: number[]) => void;
	placeholder?: string;
}

export const MultiSelectField = (props: Props) => {
	const { label, options, selected, onChange, placeholder, disabled } = props;
	const [searchTerm, setSearchTerm] = useState("");
	const [isOpen, setIsOpen] = useState(false);
	const searchInputRef = useRef<HTMLInputElement>(null);
	const containerRef = useRef<HTMLDivElement>(null);
	const triggerRef = useRef<HTMLButtonElement>(null);
	const dropdownRef = useRef<HTMLDivElement>(null);

	const filteredOptions = useMemo(() => {
		return options.filter(
			(option) => option.label.toLowerCase().includes(searchTerm.toLowerCase()) && !selected.includes(option.value),
		);
	}, [options, searchTerm, selected]);

	const selectedItems = useMemo(() => {
		return options.filter((option) => selected.includes(option.value));
	}, [options, selected]);

	const handleSelect = (value: number) => {
		onChange([...selected, value]);
		setSearchTerm("");
		setIsOpen(false);
	};

	const handleRemove = (value: number) => {
		onChange(selected.filter((v) => v !== value));
	};

	const handleToggle = () => {
		if (disabled) return;
		setIsOpen((prev) => {
			if (prev) setSearchTerm("");
			return !prev;
		});
	};

	useEffect(() => {
		if (isOpen && searchInputRef.current) {
			setTimeout(() => searchInputRef.current?.focus(), 0);
		}
	}, [isOpen]);

	useEffect(() => {
		const handleClickOutside = (event: MouseEvent) => {
			const target = event.target as Node;
			if (
				containerRef.current &&
				!containerRef.current.contains(target) &&
				dropdownRef.current &&
				!dropdownRef.current.contains(target)
			) {
				setIsOpen(false);
				setSearchTerm("");
			}
		};
		document.addEventListener("mousedown", handleClickOutside);
		return () => document.removeEventListener("mousedown", handleClickOutside);
	}, []);

	const [dropdownStyle, setDropdownStyle] = useState<React.CSSProperties>({});

	useEffect(() => {
		if (isOpen && triggerRef.current) {
			const rect = triggerRef.current.getBoundingClientRect();
			setDropdownStyle({
				position: "fixed",
				top: rect.bottom + 4,
				left: rect.left,
				width: rect.width,
				zIndex: 9999,
				pointerEvents: "auto",
			});
		}
	}, [isOpen]);

	return (
		<FormFieldWrapper label={label} errors={[]} className="w-full">
			<div className="flex flex-col gap-2" ref={containerRef}>
				<div>
					<button
						ref={triggerRef}
						type="button"
						onClick={handleToggle}
						disabled={disabled}
						className={cx(
							"w-full bg-background text-white rounded-lg border border-primary/30 focus:border-primary focus:outline-none",
							"px-4 py-2 h-[45px] inline-flex justify-between items-center text-[15px]",
							disabled && "opacity-50 cursor-not-allowed",
						)}
					>
						<span className="text-gray-400 text-sm">{placeholder}</span>
						{isOpen ? <CaretUpIcon className="size-5" /> : <CaretDownIcon className="size-5" />}
					</button>

					{isOpen &&
						createPortal(
							<div
								ref={dropdownRef}
								style={dropdownStyle}
								className="bg-input border border-primary/30 rounded-lg overflow-hidden"
							>
								<div className="p-2 border-b border-gray-700">
									<div className="flex items-center px-2 bg-input rounded-sm">
										<SearchIcon className="size-4 text-gray-400 mr-2" />
										<input
											ref={searchInputRef}
											type="text"
											value={searchTerm}
											onChange={(e) => setSearchTerm(e.target.value)}
											placeholder="Search..."
											className="bg-transparent border-none outline-hidden py-2 text-sm w-full"
										/>
									</div>
								</div>
								<div className="max-h-[300px] overflow-y-auto p-1">
									{filteredOptions.length > 0 ? (
										filteredOptions.map((option) => (
											<button
												key={option.value}
												type="button"
												onClick={() => handleSelect(option.value)}
												className="w-full text-[13px] leading-none rounded-lg h-[45px] flex items-center pl-4 hover:bg-primary/60 transition-colors cursor-pointer"
											>
												<div className="flex flex-row justify-start items-center gap-2">
													{option.image !== undefined && <BgtAvatar title={option.label} image={option.image} />}
													{option.label}
												</div>
											</button>
										))
									) : (
										<div className="text-[13px] py-2 px-4 text-gray-400">No results</div>
									)}
								</div>
							</div>,
							document.body,
						)}
				</div>

				{selectedItems.length > 0 && (
					<div className="flex flex-wrap gap-2">
						{selectedItems.map((item) => (
							<div
								key={item.value}
								className="pl-1 pr-2 py-1 bg-primary/20 border border-primary/30 rounded text-sm flex items-center gap-2"
							>
								{item.image !== undefined && <BgtAvatar title={item.label} image={item.image} size="medium" />}
								<span className="text-primary">{item.label}</span>
								<button
									type="button"
									onClick={() => handleRemove(item.value)}
									disabled={disabled}
									className="text-primary cursor-pointer hover:text-white transition-colors py-2"
								>
									<Cross className="size-3" />
								</button>
							</div>
						))}
					</div>
				)}
			</div>
		</FormFieldWrapper>
	);
};
