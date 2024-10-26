import sys
import os
from PyQt5.QtWidgets import (
    QApplication, QMainWindow, QWidget, QVBoxLayout, QHBoxLayout,
    QPushButton, QLabel, QLineEdit, QComboBox, QMessageBox,
    QFrame, QTextEdit
)
from PyQt5.QtCore import Qt, QPoint
from PyQt5.QtGui import QIcon, QFont, QPixmap
from datetime import datetime
import logging
from typing import Optional

class Logger:
    @staticmethod
    def setup_logger():
        """Set up and configure the application logger."""
        logger = logging.getLogger('ShadowStrike')
        logger.setLevel(logging.INFO)
        
        # Create logs directory if it doesn't exist
        if not os.path.exists('logs'):
            os.makedirs('logs')
            
        # File handler
        file_handler = logging.FileHandler(
            f'logs/shadowstrike_{datetime.now().strftime("%Y%m%d_%H%M%S")}.log'
        )
        file_handler.setLevel(logging.INFO)
        
        # Console handler
        console_handler = logging.StreamHandler()
        console_handler.setLevel(logging.INFO)
        
        # Create formatter
        formatter = logging.Formatter(
            '%(asctime)s - %(name)s - %(levelname)s - %(message)s'
        )
        file_handler.setFormatter(formatter)
        console_handler.setFormatter(formatter)
        
        # Add handlers to logger
        logger.addHandler(file_handler)
        logger.addHandler(console_handler)
        
        return logger

class NetworkOperations:
    @staticmethod
    def gather_recon_data(target_ip: str) -> str:
        """Gather reconnaissance data for the target IP."""
        try:
            # Placeholder for actual reconnaissance logic
            return f"Reconnaissance completed for {target_ip}\nOpen ports: [22, 80, 443]\nOS: Linux\nServices: SSH, HTTP, HTTPS"
        except Exception as e:
            raise Exception(f"Reconnaissance failed: {str(e)}")

    @staticmethod
    def start_attack(target_ip: str, target_url: str, mode: str) -> None:
        """Start the network attack with specified parameters."""
        try:
            # Placeholder for actual attack logic
            pass
        except Exception as e:
            raise Exception(f"Attack initiation failed: {str(e)}")

    @staticmethod
    def stop_attack() -> None:
        """Stop the ongoing network attack."""
        try:
            # Placeholder for attack stopping logic
            pass
        except Exception as e:
            raise Exception(f"Failed to stop attack: {str(e)}")

class ShadowStrikeGUI(QMainWindow):
    def __init__(self):
        super().__init__()
        self.logger = Logger.setup_logger()
        self.network_ops = NetworkOperations()
        self.attack_in_progress = False
        self.recon_in_progress = False
        self.drag_position = QPoint()
        
        self.init_ui()
        
    def init_ui(self):
        """Initialize the user interface."""
        self.setWindowTitle("ShadowStrike")
        self.setGeometry(100, 100, 600, 500)
        self.setWindowFlags(Qt.FramelessWindowHint)
        
        # Set icon if available
        icon_path = os.path.join('img', 'eagle32.png')
        if os.path.exists(icon_path):
            self.setWindowIcon(QIcon(icon_path))
        
        self.apply_styles()
        self.create_custom_title_bar()
        self.setup_central_widget()
        
    def apply_styles(self):
        """Apply application-wide styles."""
        self.setStyleSheet("""
            QMainWindow, QWidget {
                background-color: #1e1e2e;
                color: #cdd6f4;
                font-family: 'Segoe UI', Arial, sans-serif;
            }
            QLabel {
                font-size: 15px;
                color: #bac2de;
                padding-bottom: 2px;
            }
            QLineEdit, QComboBox {
                background-color: #313244;
                border: 1px solid #45475a;
                border-radius: 4px;
                padding: 8px 10px;
                font-size: 15px;
                color: #cdd6f4;
            }
            QLineEdit:focus, QComboBox:focus {
                border: 1px solid #89b4fa;
            }
            QPushButton {
                background-color: #89b4fa;
                color: #1e1e2e;
                border: none;
                padding: 4px 10px;
                border-radius: 4px;
                font-size: 15px;
                font-weight: bold;
            }
            QPushButton:hover {
                background-color: #b4befe;
            }
            QPushButton:pressed {
                background-color: #74c7ec;
            }
            QTextEdit {
                background-color: #313244;
                border: 1px solid #45475a;
                border-radius: 4px;
                padding: 8px;
                font-family: 'Consolas', monospace;
            }
        """)

    def create_custom_title_bar(self):
        """Create the custom title bar."""
        title_bar = QWidget(self)
        title_bar.setStyleSheet("""
            background-color: #181825;
            border-bottom: 1px solid #313244;
        """)
        
        title_layout = QHBoxLayout(title_bar)
        title_layout.setContentsMargins(10, 0, 0, 0)
        
        # Add icon if available
        icon_path = os.path.join('img', 'eagle32.png')
        if os.path.exists(icon_path):
            icon_label = QLabel()
            icon_label.setPixmap(QPixmap(icon_path))
            title_layout.addWidget(icon_label)
        
        # Title
        title_label = QLabel("ShadowStrike")
        title_label.setStyleSheet("""
            font-weight: bold;
            color: #89b4fa;
            font-size: 16px;
            padding-bottom: 3px;
        """)
        title_layout.addWidget(title_label)
        title_layout.addStretch()
        
        # Control buttons
        for button_text, slot_func in [
            ("Help", self.show_help),
            ("About", self.show_about),
            ("−", self.showMinimized),
            ("×", self.close)
        ]:
            btn = QPushButton(button_text)
            btn.setFixedSize(62, 42)
            btn.clicked.connect(slot_func)
            title_layout.addWidget(btn)
            
            if button_text in ["Help", "About"]:
                btn.setStyleSheet("""
                    QPushButton {
                        background-color: transparent;
                        color: #bac2de;
                        border: none;
                    }
                    QPushButton:hover {
                        color: #cdd6f4;
                    }
                """)
            else:
                btn.setStyleSheet("""
                    QPushButton {
                        background-color: transparent;
                        color: #bac2de;
                        border: none;
                        font-size: 16px;
                    }
                    QPushButton:hover {
                        background-color: #313244;
                    }
                """)
                if button_text == "×":
                    btn.setStyleSheet(btn.styleSheet() + """
                        QPushButton:hover {
                            background-color: #f38ba8;
                            color: #1e1e2e;
                        }
                    """)
        
        title_bar.setFixedHeight(40)
        self.layout().setMenuBar(title_bar)

    def setup_central_widget(self):
        """Set up the central widget and its layout."""
        central_widget = QWidget(self)
        self.setCentralWidget(central_widget)
        
        layout = QVBoxLayout(central_widget)
        layout.setContentsMargins(30, 30, 30, 30)
        layout.setSpacing(20)
        
        # Target inputs
        for label_text, attribute_name in [
            ("Target IP:", "target_ip_entry"),
            ("Target URL:", "target_url_entry")
        ]:
            layout.addWidget(QLabel(label_text))
            line_edit = QLineEdit()
            setattr(self, attribute_name, line_edit)
            layout.addWidget(line_edit)
        
        # Attack mode dropdown
        layout.addWidget(QLabel("Attack Mode:"))
        self.attack_mode_menu = QComboBox()
        self.attack_mode_menu.addItems([
            "SYN Flood", "HTTP DDoS", "Stealth Probe", "Custom Exploit"
        ])
        layout.addWidget(self.attack_mode_menu)
        
        # Action buttons
        self.create_action_buttons(layout)
        
        # Log display
        self.log_textbox = QTextEdit()
        self.log_textbox.setReadOnly(True)
        layout.addWidget(self.log_textbox)
        
        layout.addStretch()

    def create_action_buttons(self, layout):
        """Create and arrange action buttons."""
        button_layout = QHBoxLayout()
        
        # Recon button
        self.recon_button = QPushButton("Start Recon")
        self.recon_button.clicked.connect(self.start_recon)
        layout.addWidget(self.recon_button)
        
        # Attack control buttons
        self.start_button = QPushButton("Start Attack")
        self.stop_button = QPushButton("Stop Attack")
        self.stop_button.setStyleSheet("""
            background-color: #f38ba8;
            color: #1e1e2e;
        """)
        self.start_button.clicked.connect(self.start_attack)
        self.stop_button.clicked.connect(self.stop_attack)
        self.stop_button.setEnabled(False)
        
        button_layout.addWidget(self.start_button)
        button_layout.addWidget(self.stop_button)
        layout.addLayout(button_layout)

    def show_message(self, title: str, message: str, icon: QMessageBox.Icon = QMessageBox.Information):
        """Show a message box with the specified parameters."""
        msg_box = QMessageBox(self)
        msg_box.setIcon(icon)
        msg_box.setWindowTitle(title)
        msg_box.setText(message)
        msg_box.exec_()

    def validate_input(self) -> bool:
        """Validate user input before starting operations."""
        target_ip = self.target_ip_entry.text().strip()
        target_url = self.target_url_entry.text().strip()
        
        if not target_ip and not target_url:
            self.show_message(
                "Error",
                "Please enter either a Target IP or a Target URL.",
                QMessageBox.Critical
            )
            return False
        return True

    def start_attack(self):
        """Start the attack operation."""
        if not self.validate_input():
            return
            
        if self.attack_in_progress:
            self.show_message(
                "Error",
                "An attack is already in progress.",
                QMessageBox.Critical
            )
            return
            
        try:
            self.attack_in_progress = True
            self.update_ui_state()
            
            NetworkOperations.start_attack(
                self.target_ip_entry.text().strip(),
                self.target_url_entry.text().strip(),
                self.attack_mode_menu.currentText()
            )
            
            self.log_message("Attack started successfully")
            self.show_message("Success", "Attack initiated successfully!")
            
        except Exception as e:
            self.attack_in_progress = False
            self.update_ui_state()
            self.log_message(f"Error starting attack: {str(e)}", error=True)
            self.show_message("Error", f"Failed to start attack: {str(e)}", QMessageBox.Critical)

    def stop_attack(self):
        """Stop the attack operation."""
        if not self.attack_in_progress:
            self.show_message(
                "Error",
                "No attack is currently in progress.",
                QMessageBox.Critical
            )
            return
            
        try:
            NetworkOperations.stop_attack()
            self.attack_in_progress = False
            self.update_ui_state()
            self.log_message("Attack stopped successfully")
            self.show_message("Success", "Attack stopped successfully.")
            
        except Exception as e:
            self.log_message(f"Error stopping attack: {str(e)}", error=True)
            self.show_message("Error", f"Failed to stop attack: {str(e)}", QMessageBox.Critical)

    def start_recon(self):
        """Start the reconnaissance operation."""
        target_ip = self.target_ip_entry.text().strip()
        if not target_ip:
            self.show_message(
                "Error",
                "Please enter a Target IP for reconnaissance.",
                QMessageBox.Critical
            )
            return
            
        if self.recon_in_progress:
            self.show_message(
                "Error",
                "Reconnaissance is already in progress.",
                QMessageBox.Critical
            )
            return
            
        try:
            self.recon_in_progress = True
            self.recon_button.setEnabled(False)
            
            recon_data = NetworkOperations.gather_recon_data(target_ip)
            self.log_message(f"Reconnaissance data gathered:\n{recon_data}")
            
        except Exception as e:
            self.log_message(f"Error during reconnaissance: {str(e)}", error=True)
            self.show_message("Error", f"Failed to gather reconnaissance data: {str(e)}", QMessageBox.Critical)
            
        finally:
            self.recon_in_progress = False
            self.recon_button.setEnabled(True)

    def update_ui_state(self):
        """Update UI elements based on current state."""
        self.stop_button.setEnabled(self.attack_in_progress)
        self.start_button.setEnabled(not self.attack_in_progress)
        self.recon_button.setEnabled(not self.recon_in_progress)

    def log_message(self, message: str, error: bool = False):
        """Log a message to both the GUI and the logger."""
        timestamp = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
        log_entry = f"[{timestamp}] {'ERROR: ' if error else ''}{message}"
        
        self.log_textbox.append(log_entry)
        if error:
            self.logger.error(message)
        else:
            self.logger.info(message)

    def show_help(self):
            """Show the help dialog."""
            help_text = """
How to Use ShadowStrike:

1. Enter the Target IP or URL
2. Select the Attack Mode from the dropdown menu
3. Click 'Start Attack' to initiate the attack
4. Click 'Stop Attack' to stop an ongoing attack

Note: Ensure you have the necessary permissions before running attacks.
"""
            self.show_message("Help", help_text)

    def show_about(self):
        """Show the about dialog."""
        about_text = """
ShadowStrike

Developer: Shankar Aryal
Version: 1.0.1

A network security testing application with attack simulation 
and reconnaissance capabilities.

For more information, visit our GitHub repository or 
contact the developer.

© 2024 All rights reserved
"""
        self.show_message("About", about_text)


    def mousePressEvent(self, event):
        """Handle mouse press events for window dragging."""
        if event.button() == Qt.LeftButton:
            self.drag_position = event.globalPos() - self.frameGeometry().topLeft()
            event.accept()

    def mouseMoveEvent(self, event):
        """Handle mouse move events for window dragging."""
        if event.buttons() == Qt.LeftButton:
            self.move(event.globalPos() - self.drag_position)
            event.accept()

def main():
    """Main application entry point."""
    try:
        app = QApplication(sys.argv)
        
        # Create logs directory if it doesn't exist
        os.makedirs('logs', exist_ok=True)
        
        # Create main window
        gui = ShadowStrikeGUI()
        gui.show()
        
        # Start application event loop
        sys.exit(app.exec_())
        
    except Exception as e:
        logging.error(f"Application failed to start: {str(e)}")
        QMessageBox.critical(None, "Error", f"Application failed to start: {str(e)}")
        sys.exit(1)

if __name__ == "__main__":
    main()